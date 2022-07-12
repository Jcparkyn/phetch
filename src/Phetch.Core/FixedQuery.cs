namespace Phetch.Core;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class FixedQuery<TResult>
{
    private readonly IQueryCache<TResult> _queryCache;
    private readonly Func<Task<TResult>> _queryFn;
    private readonly List<IQueryObserver<TResult>> _observers = new();
    private readonly TimeSpan _cacheTime;

    private Task<TResult>? _lastActionCall;
    private bool _isInvalidated = false;
    private Timer? _gcTimer;
    private DateTime? _dataUpdatedAt;
    private DateTime? _lastCompletedTaskStartTime;

    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    public TResult? Data { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsFetching => _lastActionCall is not null && !_lastActionCall.IsCompleted;

    public FixedQuery(
        IQueryCache<TResult> queryCache,
        Func<Task<TResult>> queryFn,
        QueryEndpointOptions<TResult> options)
    {
        _queryCache = queryCache;
        _queryFn = queryFn;
        _cacheTime = options.CacheTime;
    }

    public void UpdateQueryData(TResult? resultData)
    {
        Data = resultData;
        _dataUpdatedAt = DateTime.Now;
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate(QueryEvent.Other, default, null);
        }
    }

    public bool IsStaleByTime(TimeSpan staleTime, DateTime now)
    {
        return _isInvalidated
            || _dataUpdatedAt is null
            || _dataUpdatedAt + staleTime < now;
    }

    public void Invalidate()
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

    public void Refetch() => _ = RefetchAsync();

    public async Task<TResult?> RefetchAsync()
    {
        if (Status != QueryStatus.Success)
        {
            Status = QueryStatus.Loading;
        }

        var startTime = DateTime.UtcNow;

        Task<TResult>? thisActionCall = null;
        try
        {
            thisActionCall = _queryFn();
            _lastActionCall = thisActionCall;
            var newData = await thisActionCall;
            // Only update if no more recent tasks have finished.
            if (IsMostRecent(startTime))
            {
                SetSuccessState(newData, startTime);
            }
            return newData;
        }
        catch (Exception ex)
        {
            if (IsMostRecent(startTime))
            {
                Error = ex;
                Status = QueryStatus.Error;
                _lastCompletedTaskStartTime = startTime;
                foreach (var observer in _observers)
                {
                    observer.OnQueryUpdate(QueryEvent.Error, default, ex);
                }
            }

            throw;
        }
    }

    internal void AddObserver(IQueryObserver<TResult> observer)
    {
        _observers.Add(observer);
        UnscheduleGc();
    }

    internal void RemoveObserver(IQueryObserver<TResult> observer)
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
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate(QueryEvent.Success, newData, null);
        }
    }

    private void ScheduleGc()
    {
        _gcTimer?.Dispose();
        if (_cacheTime > TimeSpan.Zero)
            _gcTimer = new Timer(GcTimerCallback, null, _cacheTime, Timeout.InfiniteTimeSpan);
    }

    private void UnscheduleGc()
    {
        _gcTimer?.Dispose();
        _gcTimer = null;
    }

    private void GcTimerCallback(object _) => Cleanup();

    private void Cleanup()
    {
        _gcTimer?.Dispose();
        _gcTimer = null;
        _queryCache.Remove(this);
    }
}
