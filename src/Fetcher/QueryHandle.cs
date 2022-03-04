namespace Fetcher;

using System;

public class QueryHandle<TArg, TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;
    private FixedQuery<TResult>? _currentQuery;

    public event Action StateChanged = delegate { };

    public QueryStatus Status => _currentQuery?.Status ?? QueryStatus.Idle;

    public FixedQuery<TResult>? Query => _currentQuery;

    public QueryHandle(
        QueryCache<TArg, TResult> cache)
    {
        _cache = cache;
    }

    public void Refetch()
    {
        _currentQuery?.Refetch();
    }

    public void SetParams(TArg arg, bool forceLoad = false)
    {
        var newQuery = _cache.GetOrAdd(arg);
        if (newQuery != _currentQuery)
        {
            if (_currentQuery is not null)
            {
                _currentQuery.StateChanged -= OnStateChanged;
            }
            newQuery.StateChanged += OnStateChanged;
        }
        _currentQuery = newQuery;
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke();
    }
}
