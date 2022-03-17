namespace Fetcher;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

internal interface IQueryObserver<TResult>
{
    internal void OnQueryUpdate(QueryEvent e, TResult? result, Exception? exception);
}

internal enum QueryEvent
{
    Other,
    Success,
    Error,
}

public class QueryObserver<TArg, TResult> : IQueryObserver<TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;
    private readonly QueryObserverOptions<TResult> _options;
    private FixedQuery<TResult>? _currentQuery;

    public event Action StateChanged = delegate { };

    public QueryStatus Status => _currentQuery?.Status ?? QueryStatus.Idle;

    public TResult? Data => _currentQuery is null
        ? default
        : _currentQuery.Data;

    public FixedQuery<TResult>? Query => _currentQuery;

    public QueryObserver(
        QueryCache<TArg, TResult> cache,
        QueryObserverOptions<TResult> options)
    {
        _cache = cache;
        _options = options;
    }

    public QueryObserver(
        Func<TArg, Task<TResult>> queryFn,
        QueryObserverOptions<TResult> options
    ) : this(new QueryCache<TArg, TResult>(queryFn, null), options) { }

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
            _currentQuery = newQuery;

            if (newQuery.IsStale)
            {
                newQuery.Refetch();
            }
        }
    }

    public void Detach()
    {
        // TODO: Consider redesign
        _currentQuery?.RemoveObserver(this);
        _currentQuery = null;
    }

    void IQueryObserver<TResult>.OnQueryUpdate(QueryEvent e, TResult? result, Exception? exception)
    {
        switch (e)
        {
            case QueryEvent.Success:
                _options.OnSuccess?.Invoke(result);
                break;
            case QueryEvent.Error:
                _options.OnFailure?.Invoke(exception!);
                break;
        }
        StateChanged?.Invoke();
    }
}
