namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class FixedQuery<TResult>
{
    private readonly Func<Task<TResult>> _queryFn;
    private readonly HashSet<IQueryObserver<TResult>> _observers = new();

    private Task<TResult>? _lastActionCall;

    //public event Action? StateChanged = delegate { };

    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    public TResult? Data { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsLoading => Status == QueryStatus.Loading;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null && Status == QueryStatus.Error;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && Status == QueryStatus.Success;

    public bool IsUninitialized => Status == QueryStatus.Idle;

    public bool IsFetching => IsLoading || (_lastActionCall is not null && !_lastActionCall.IsCompleted);

    public FixedQuery(
        Func<Task<TResult>> queryFn,
        TResult? initialData = default)
    {
        _queryFn = queryFn;
        Data = initialData;
        Refetch();
    }

    public void UpdateQueryData(TResult? resultData)
    {
        Data = resultData;
        NotifyStateChange();
    }

    public void Invalidate()
    {
        if (_observers.Count > 0)
        {
            Refetch();
        }
        // TODO: Track stale status
    }

    public void Refetch() => _ = RefetchAsync();

    public async Task<TResult?> RefetchAsync()
    {
        if (Status != QueryStatus.Success)
        {
            Status = QueryStatus.Loading;
        }

        NotifyStateChange(); // TODO: Avoid unnecessary re-renders

        Task<TResult>? thisActionCall = null;
        try
        {
            thisActionCall = _queryFn();
            _lastActionCall = thisActionCall;
            var newData = await thisActionCall;
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                SetSuccessState(newData);
            }
            return newData;
        }
        catch (Exception ex)
        {
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                Error = ex;
                Status = QueryStatus.Error;
                NotifyStateChange();
            }

            throw;
        }
    }

    internal void AddObserver(IQueryObserver<TResult> observer)
    {
        _observers.Add(observer);
    }

    internal void RemoveObserver(IQueryObserver<TResult> observer)
    {
        _observers.Remove(observer);
    }

    private void NotifyStateChange()
    {
        foreach (var observer in _observers)
        {
            observer.OnQueryUpdate();
        }
        //StateChanged?.Invoke();
    }

    private void SetSuccessState(TResult? newData)
    {
        Status = QueryStatus.Success;
        Data = newData;
        Error = null;
        NotifyStateChange();
    }
}
