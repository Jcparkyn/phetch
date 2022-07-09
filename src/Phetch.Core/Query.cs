namespace Phetch.Core;

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
public class Query<TArg, TResult> : IQueryObserver<TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;
    private readonly QueryOptions<TResult> _options;
    private FixedQuery<TResult>? _lastSuccessfulQuery;
    private FixedQuery<TResult>? _currentQuery;

    public event Action StateChanged = delegate { };

    public QueryStatus Status => _currentQuery?.Status ?? QueryStatus.Idle;

    public TResult? Data => _currentQuery is not null
        ? _currentQuery.Data
        : default;

    /// <summary>
    /// The response data from the current query if it exists, otherwise the response data from the
    /// last successful query.
    /// </summary>
    /// <remarks>
    /// This is useful for pagination, if you want to keep the data of the previous page visible
    /// while the next page loads. May return data from a different set of parameters if the
    /// parameters have changed.
    /// </remarks>
    public TResult? LastData => IsSuccess
        ? _currentQuery.Data
        : _lastSuccessfulQuery?.Status == QueryStatus.Success
            ? _lastSuccessfulQuery.Data
            : default;

    /// <summary>
    /// The exception returned by the last query failure, or <c>null</c> if the query has never failed.
    /// </summary>
    public Exception? Error => _currentQuery?.Error;

    /// <summary>
    /// True if the query is currently loading and has not previously succeeded with the same parameters.
    /// </summary>
    /// <remarks>
    /// This will return <c>false</c> if the query is currently re-fetching due to the current data
    /// being stale. Use <see cref="IsFetching"/> for these cases (e.g., to show a loading indicator).
    /// </remarks>
    public bool IsLoading => _currentQuery?.Status == QueryStatus.Loading;

    /// <summary>
    /// True if the query threw an exception and has not been re-run.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Status == QueryStatus.Error;

    /// <summary>
    /// True if the query has succeeded.
    /// </summary>
    /// <remarks>
    /// In many cases you should prefer to use <see cref="HasData"/> as it works better with
    /// nullable reference types.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(_currentQuery))]
    public bool IsSuccess => _currentQuery?.Status == QueryStatus.Success;

    /// <summary>
    /// True if the query has succeeded and returned a non-null response.
    /// </summary>
    /// <remarks>
    /// This is particularly useful in combination with nullable reference types, as it lets you
    /// safely access <see cref="Data"/> without a compiler warning.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool HasData => IsSuccess && Data is not null;

    /// <summary>
    /// True if no parameters have been provided to this query yet.
    /// </summary>
    public bool IsUninitialized => Status == QueryStatus.Idle;

    /// <summary>
    /// True if the query is currently running, either for the initial load or for subsequent
    /// fetches once the data is stale.
    /// </summary>
    /// <remarks>
    /// If you only need to know about the initial load, use <see cref="IsLoading"/> instead.
    /// </remarks>
    public bool IsFetching => _currentQuery?.IsFetching ?? false;

    public Query(
        QueryCache<TArg, TResult> cache,
        QueryOptions<TResult>? options = null)
    {
        _cache = cache;
        _options = options ?? new();
    }

    public Query(
        Func<TArg, Task<TResult>> queryFn,
        QueryOptions<TResult>? options = null
    ) : this(new QueryCache<TArg, TResult>(queryFn, null), options) { }

    /// <summary>
    /// Run the query using the most recent parameters.
    /// </summary>
    /// <remarks>
    /// To also return the result of the query, use <see cref="RefetchAsync"/>.
    /// </remarks>
    public void Refetch()
    {
        _currentQuery?.Refetch();
    }

    /// <summary>
    /// Re-run the query using the most recent parameters and return the result asynchronously.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task<TResult?> RefetchAsync()
    {
        if (_currentQuery is null)
            throw new InvalidOperationException("Cannot refetch an unititialized query");

        return _currentQuery.RefetchAsync();
    }

    /// <summary>
    /// Update the parameters of this query, and re-run the query if the parameters have changed.
    /// </summary>
    /// <remarks>
    /// If you need to <c>await</c> the completion of the query, use <see cref="SetParamAsync(TArg)"/> instead.
    /// </remarks>
    public void SetParam(TArg arg) => _ = SetParamAsync(arg);

    /// <summary>
    /// Update the parameters of this query, and re-run the query if the parameters have changed.
    /// </summary>
    /// <remarks>
    /// If you do not need to <c>await</c> the completion of the query, use <see cref="SetParam(TArg)"/> instead.
    /// </remarks>
    /// <returns>
    /// A <see cref="Task"/> which completes when the query returns, or immediately if the
    /// parameters have not changed.
    /// </returns>
    public async Task SetParamAsync(TArg arg)
    {
        var newQuery = _cache.GetOrAdd(arg);
        if (newQuery != _currentQuery)
        {
            _currentQuery?.RemoveObserver(this);
            newQuery.AddObserver(this);
            _currentQuery = newQuery;

            if (newQuery.IsStaleByTime(_options.StaleTime, DateTime.Now))
            {
                await newQuery.RefetchAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Stop listening to changes of the current query.
    /// </summary>
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
        QueryOptions<TResult>? options = null,
        bool runAutomatically = true
    ) : base(_ => queryFn(), options)
    {
        if (runAutomatically)
        {
            SetParam(default); // Trigger an initial query
        }
    }

    public Query(
        QueryCache<Unit, TResult> cache,
        QueryOptions<TResult>? options = null,
        bool runAutomatically = true
    ) : base(cache, options)
    {
        if (runAutomatically)
        {
            SetParam(default); // Trigger an initial query
        }
    }
}
