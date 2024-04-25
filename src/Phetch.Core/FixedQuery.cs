namespace Phetch.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A class representing a query with a single (fixed) query argument.
/// </summary>
public sealed class FixedQuery<TArg, TResult> : IDisposable
{
    /// <summary>
    /// The argument that was passed to this query.
    /// </summary>
    public TArg Arg { get; }

    private readonly QueryCache<TArg, TResult> _queryCache;
    private readonly Func<TArg, CancellationToken, Task<TResult>> _queryFn;
    private readonly EndpointOptions<TArg, TResult> _endpointOptions;
    private readonly List<Query<TArg, TResult>> _observers = new();

    private Task<TResult>? _lastActionCall;
    private bool _isInvalidated;
    private Timer? _gcTimer;
    private DateTime? _dataUpdatedAt;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Undisposed CTSs are safe, and disposing would potentially cause issues with ongoing queries.")]
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// The current status of this query.
    /// </summary>
    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    /// <summary>
    /// The data value returned from this query, or <c>default</c> it hasn't returned yet.
    /// </summary>
    public TResult? Data { get; private set; }

    /// <summary>
    /// The exception thrown the last time this query failed, or <c>null</c> it has never failed.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// True if the query is currently running, either for the initial load or for subsequent
    /// fetches once the data is stale.
    /// </summary>
    public bool IsFetching => _lastActionCall is { IsCompleted: false };

    internal Task<TResult>? LastInvocation { get; private set; }

    // Separate from Status because queries can succeed then fail on a refetch.
    internal bool HasSucceeded { get; private set; }

    internal FixedQuery(
        QueryCache<TArg, TResult> queryCache,
        Func<TArg, CancellationToken, Task<TResult>> queryFn,
        TArg arg,
        EndpointOptions<TArg, TResult> endpointOptions)
    {
        _queryCache = queryCache;
        _queryFn = queryFn;
        Arg = arg;
        _endpointOptions = endpointOptions;
    }

    /// <summary>
    /// Replaces the data for this cache entry. Any components using it will automatically receive
    /// the updated data.
    /// </summary>
    public void UpdateQueryData(TResult resultData)
    {
        // Updating LastInvocation makes the API a bit more consistent
        LastInvocation = Task.FromResult(resultData);
        SetSuccessState(resultData);
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
    }

    internal bool IsStaleByTime(TimeSpan staleTime, DateTime now)
    {
        return _isInvalidated
            || _dataUpdatedAt is null
            || staleTime == TimeSpan.Zero
            // Comparison order is important to avoid overflow with TimeSpan.MaxValue
            // Note: This is safe even if (now < _dataUpdatedAt)
            || (staleTime > TimeSpan.Zero && staleTime != TimeSpan.MaxValue && (now - _dataUpdatedAt > staleTime));
    }

    /// <summary>
    /// Invalidates this cache entry, causing it to be re-fetched if it is currently being used. If
    /// this cache entry is unused, it will be marked as invalidated and re-fetched as soon as it
    /// becomes used.
    /// </summary>
    public void Invalidate()
    {
        if (_observers.Count > 0)
        {
            _ = RefetchAsync(retryHandler: null);
        }
        else
        {
            _isInvalidated = true;
        }
    }

    internal void Cancel()
    {
        if (Status == QueryStatus.Loading)
        {
            Status = QueryStatus.Idle;
        }
        RequestCancellation();
        _lastActionCall = null;
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
    }

    private void RequestCancellation()
    {
        if (IsFetching)
        {
            try
            {
                _cts.Cancel();
            }
            finally
            {
                _cts = new();
            }
        }
    }

    [DebuggerStepThrough]
    internal Task<TResult> RefetchAsync(IRetryHandler? retryHandler)
    {
        var task = RefetchAsyncImpl(retryHandler);
        LastInvocation = task;
        return task;
    }

    // This is a separate method to allow the resulting Task to be stored and used by other methods
    private async Task<TResult> RefetchAsyncImpl(IRetryHandler? retryHandler)
    {
        if (Status != QueryStatus.Success)
        {
            Status = QueryStatus.Loading;
        }

        var startTime = DateTime.UtcNow;
        var thisCts = _cts; // Save _cts ahead of time so we can check the same reference later

        Task<TResult>? thisActionCall = null;
        try
        {
            var token = thisCts.Token;
            thisActionCall = (retryHandler ?? _endpointOptions.RetryHandler) switch
            {
                null or NoRetryHandler => _queryFn(Arg, thisCts.Token),
                IRetryHandler handler => handler.ExecuteAsync(ct => _queryFn(Arg, ct), token),
            };

            _lastActionCall = thisActionCall;
            if (!thisActionCall.IsCompleted)
            {
                foreach (var observer in _observers)
                {
                    observer.OnQueryUpdate();
                }
            }
            var newData = await thisActionCall;
            // Ignore results from cancelled calls, these have already been handled.
            if (!thisCts.IsCancellationRequested)
            {
                SetSuccessState(newData);
                NotifySuccess(newData);
            }
            return newData;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == thisCts.Token)
        {
            // Do nothing when the cancellation is caught.
            // The state change has already been handled by Cancel()
            throw;
        }
        catch (Exception ex) when (!thisCts.IsCancellationRequested)
        {
            Error = ex;
            Status = QueryStatus.Error;
            NotifyFailure(ex);
            throw;
        }
    }

    internal void AddObserver(Query<TArg, TResult> observer)
    {
        _observers.Add(observer);
        UnscheduleGc();
    }

    internal void RemoveObserver(Query<TArg, TResult> observer)
    {
        _observers.Remove(observer);

        if (_observers.Count == 0)
            ScheduleGc();
    }

    private void SetSuccessState(TResult? newData)
    {
        _isInvalidated = false;
        _dataUpdatedAt = DateTime.Now;
        Status = QueryStatus.Success;
        HasSucceeded = true;
        Data = newData;
        Error = null;
    }

    private void NotifySuccess(TResult newData)
    {
        var eventArgs = new QuerySuccessEventArgs<TArg, TResult>(Arg, newData);

#pragma warning disable CA1031 // Do not catch general exception types
        // There isn't much we can do if these callbacks throw, so we just swallow any exceptions,
        // to ensure that all callbacks are called.
        // Otherwise, they are caught in RefetchAsyncImpl and treated as a query failure (incorrectly).
        try
        {
            _endpointOptions.OnSuccess?.Invoke(eventArgs);
        }
        catch { }
        foreach (var observer in _observers)
        {
            try
            {
                observer.OnQuerySuccess(eventArgs);
            }
            catch { }
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private void NotifyFailure(Exception ex)
    {
        var eventArgs = new QueryFailureEventArgs<TArg>(Arg, ex);

#pragma warning disable CA1031 // Do not catch general exception types
        // There isn't much we can do if these callbacks throw, so we just swallow any exceptions,
        // to ensure that all callbacks are called.
        try
        {
            _endpointOptions.OnFailure?.Invoke(eventArgs);
        }
        catch { }
        foreach (var observer in _observers)
        {
            try
            {
                observer.OnQueryFailure(eventArgs);
            }
            catch { }
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private void ScheduleGc()
    {
        var cacheTime = _endpointOptions.CacheTime;
        _gcTimer?.Dispose();
        if (cacheTime > TimeSpan.Zero && cacheTime != TimeSpan.MaxValue)
        {
            _gcTimer = new Timer(GcTimerCallback, null, cacheTime, Timeout.InfiniteTimeSpan);
        }
        else if (cacheTime == TimeSpan.Zero)
        {
            Dispose();
        }
    }

    private void UnscheduleGc()
    {
        _gcTimer?.Dispose();
        _gcTimer = null;
    }

    private void GcTimerCallback(object _) => this.Dispose();

    /// <summary>
    /// Removes this query from the cache.
    /// </summary>
    public void Dispose()
    {
        _gcTimer?.Dispose();
        _gcTimer = null;
        _queryCache.Remove(this);
    }
}
