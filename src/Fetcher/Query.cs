namespace Fetcher;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

internal interface IQuery<TResult>
{
    internal void OnQueryUpdate(QueryEvent e, TResult? result, Exception? exception);
}

internal enum QueryEvent
{
    Other,
    Success,
    Error,
}

/// <summary>
/// An asynchronous query taking one parameter of type <typeparamref name="TArg"/> and returning a
/// result of type <typeparamref name="TResult"/>
/// </summary>
/// <remarks>
/// <para>For queries with no parameters, you can use the <see cref="Query{TResult}"/> class.</para>
/// <para>For queries with multiple parameters, you can use a tuple in place of <c>TArg</c>:
/// <code>Query&lt;(int, string), string&gt;</code>
/// </para>
/// </remarks>
public class Query<TArg, TResult> : IQuery<TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;
    private readonly QueryObserverOptions<TResult> _options;
    private FixedQuery<TResult>? _lastSuccessfulQuery;
    private FixedQuery<TResult>? _currentQuery;

    public event Action StateChanged = delegate { };

    public QueryStatus Status => _currentQuery?.Status ?? QueryStatus.Idle;

    public TResult? Data => _currentQuery is not null
        ? _currentQuery.Data
        : default;

    public TResult? LastData => IsSuccess
        ? _currentQuery.Data
        : _lastSuccessfulQuery?.Status == QueryStatus.Success
            ? _lastSuccessfulQuery.Data
            : default;

    public Exception? Error => _currentQuery?.Error;

    public bool IsLoading => Status == QueryStatus.Loading;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Status == QueryStatus.Error;

    [MemberNotNullWhen(true, nameof(_currentQuery))]
    public bool IsSuccess => _currentQuery?.Status == QueryStatus.Success;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool HasData => Data is not null; // TODO: support value types

    public bool IsUninitialized => Status == QueryStatus.Idle;

    public bool IsFetching => _currentQuery?.IsFetching ?? false;

    public Query(
        QueryCache<TArg, TResult> cache,
        QueryObserverOptions<TResult>? options = null)
    {
        _cache = cache;
        _options = options ?? new();
    }

    public Query(
        Func<TArg, Task<TResult>> queryFn,
        QueryObserverOptions<TResult>? options = null
    ) : this(new QueryCache<TArg, TResult>(queryFn, null), options) { }

    public void Refetch()
    {
        _currentQuery?.Refetch();
    }

    public Task<TResult?> RefetchAsync()
    {
        if (_currentQuery is null)
            throw new InvalidOperationException("Cannot refetch an unititialized query");

        return _currentQuery.RefetchAsync();
    }

    public void SetParams(TArg arg) => _ = SetParamsAsync(arg);

    public async Task SetParamsAsync(TArg arg)
    {
        var newQuery = _cache.GetOrAdd(arg);
        if (newQuery != _currentQuery)
        {
            _currentQuery?.RemoveObserver(this);
            newQuery.AddObserver(this);
            _currentQuery = newQuery;

            if (newQuery.IsStaleByTime(_options.StaleTime))
            {
                await newQuery.RefetchAsync().ConfigureAwait(false);
            }
        }
    }

    public void Detach()
    {
        // TODO: Consider redesign
        _currentQuery?.RemoveObserver(this);
        _currentQuery = null;
    }

    void IQuery<TResult>.OnQueryUpdate(QueryEvent e, TResult? result, Exception? exception)
    {
        switch (e)
        {
            case QueryEvent.Success:
                _lastSuccessfulQuery = _currentQuery;
                _options.OnSuccess?.Invoke(result);
                break;
            case QueryEvent.Error:
                _options.OnFailure?.Invoke(exception!);
                break;
        }
        StateChanged?.Invoke();
    }
}

public class Query<TResult> : Query<Unit, TResult>
{
    public Query(
        Func<Task<TResult>> queryFn,
        QueryObserverOptions<TResult>? options = null,
        bool runAutomatically = true
    ) : base(_ => queryFn(), options)
    {
        if (runAutomatically)
        {
            SetParams(default); // Trigger an initial query
        }
    }
}
