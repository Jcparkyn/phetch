namespace Fetcher;

using System;

internal interface IQueryObserver<TResult>
{
    internal void OnQueryUpdate();
}

public class QueryObserver<TArg, TResult> : IQueryObserver<TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;
    private FixedQuery<TResult>? _currentQuery;

    public event Action StateChanged = delegate { };

    public QueryStatus Status => _currentQuery?.Status ?? QueryStatus.Idle;

    public FixedQuery<TResult>? Query => _currentQuery;

    public QueryObserver(
        QueryCache<TArg, TResult> cache)
    {
        _cache = cache;
    }

    public void Refetch()
    {
        _currentQuery?.Refetch();
    }

    public void SetParams(TArg arg)
    {
        var newQuery = _cache.GetOrAdd(arg);
        if (newQuery != _currentQuery)
        {
            _currentQuery?.RemoveObserver(this);
            newQuery.AddObserver(this);
        }
        _currentQuery = newQuery;
    }

    void IQueryObserver<TResult>.OnQueryUpdate()
    {
        StateChanged?.Invoke();
    }
}
