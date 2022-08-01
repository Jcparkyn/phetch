namespace Phetch.Core;

using System;
using System.Collections.Generic;
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

    internal void UpdateQueryData(TResult? resultData)
    {
        Data = resultData;
        _dataUpdatedAt = DateTime.Now;
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
    }

    internal bool IsStaleByTime(TimeSpan staleTime, DateTime now)
    {
        return _isInvalidated
            || _dataUpdatedAt is null
            || _dataUpdatedAt + staleTime < now;
    }

    internal void Invalidate()
    {
        if (_observers.Count > 0)
        {
            Refetch();
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

    internal void Refetch() => _ = RefetchAsync();

    internal async Task<TResult> RefetchAsync()
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
            thisActionCall = _queryFn(Arg, thisCts.Token);
            _lastActionCall = thisActionCall;
            var newData = await thisActionCall;
            // Only update if no more recent tasks have finished.
            if (IsMostRecent(startTime) && !thisCts.IsCancellationRequested)
            {
                SetSuccessState(newData, startTime);
                var context = new QuerySuccessContext<TArg, TResult>(Arg, newData);
                _endpointOptions.OnSuccess?.Invoke(context);
                foreach (var observer in _observers)
                {
                    observer.OnQuerySuccess(context);
                }
            }
            return newData;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == thisCts.Token)
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
                var context = new QueryFailureContext<TArg>(Arg, ex);
                _endpointOptions.OnFailure?.Invoke(context);
                foreach (var observer in _observers)
                {
                    observer.OnQueryFailure(context);
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
