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
    private DateTime? _lastCompletedTaskStartTime;
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
    public bool IsFetching => _lastActionCall is not null && !_lastActionCall.IsCompleted;

    internal Task<TResult>? LastInvokation { get; private set; }

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
        // Updating LastInvokation makes the API a bit more consistent
        LastInvokation = Task.FromResult(resultData);
        SetSuccessState(resultData, DateTime.Now);
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
    }

    internal bool IsStaleByTime(TimeSpan staleTime, DateTime now)
    {
        return _isInvalidated
            || _dataUpdatedAt is null
            // Comparison order is important to avoid overflow with TimeSpan.MaxValue
            // Note: This is safe even if (now < _dataUpdatedAt)
            || now - _dataUpdatedAt > staleTime;
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
            Refetch(retryHandler: null);
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
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
        RequestCancellation();
        _lastActionCall = null;
    }

    private void RequestCancellation()
    {
        if (_lastActionCall is not null && !_lastActionCall.IsCompleted)
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

    internal void Refetch(IRetryHandler? retryHandler) => _ = RefetchAsync(retryHandler);

    [DebuggerStepThrough]
    internal Task<TResult> RefetchAsync(IRetryHandler? retryHandler)
    {
        var task = RefetchAsyncImpl(retryHandler);
        LastInvokation = task;
        return task;
    }

    // This is a separate method to allow the resulting Task to be stored and poten
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
                { } handler when handler is not NoRetryHandler => handler.ExecuteAsync(ct => _queryFn(Arg, ct), token),
                _ => _queryFn(Arg, thisCts.Token),
            };

            _lastActionCall = thisActionCall;
            var newData = await thisActionCall;
            // Only update if no more recent tasks have finished.
            if (IsMostRecent(startTime) && !thisCts.IsCancellationRequested)
            {
                SetSuccessState(newData, startTime);
                var eventArgs = new QuerySuccessEventArgs<TArg, TResult>(Arg, newData);
                _endpointOptions.OnSuccess?.Invoke(eventArgs);
                foreach (var observer in _observers)
                {
                    observer.OnQuerySuccess(eventArgs);
                }
            }
            return newData;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == thisCts.Token)
        {
            // Do nothing when the cancellation is caught.
            // The state change has already been handled by Cancel()
            throw;
        }
        catch (Exception ex)
        {
            if (IsMostRecent(startTime) && !thisCts.IsCancellationRequested)
            {
                Error = ex;
                Status = QueryStatus.Error;
                _lastCompletedTaskStartTime = startTime;
                var eventArgs = new QueryFailureEventArgs<TArg>(Arg, ex);
                _endpointOptions.OnFailure?.Invoke(eventArgs);
                foreach (var observer in _observers)
                {
                    observer.OnQueryFailure(eventArgs);
                }
            }

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

    // We only want to use the "most recent" data, based on the time that the request was made.
    // This avoids race conditions when queries return in a different order than they were made.
    private bool IsMostRecent(DateTime startTime) =>
        _lastCompletedTaskStartTime == null || startTime > _lastCompletedTaskStartTime;

    private void SetSuccessState(TResult? newData, DateTime startTime)
    {
        _isInvalidated = false;
        _dataUpdatedAt = DateTime.Now;
        _lastCompletedTaskStartTime = startTime;
        Status = QueryStatus.Success;
        Data = newData;
        Error = null;
    }

    private void ScheduleGc()
    {
        var cacheTime = _endpointOptions.CacheTime;
        _gcTimer?.Dispose();
        if (cacheTime > TimeSpan.Zero)
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
