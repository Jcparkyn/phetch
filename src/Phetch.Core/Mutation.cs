namespace Phetch.Core;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

public class Mutation<TArg, TResult> : IQueryObserver<TResult>
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

    public Mutation(
        QueryCache<TArg, TResult> cache,
        QueryOptions<TResult>? options = null)
    {
        _cache = cache;
        _options = options ?? new();
    }

    public Mutation(
        Func<TArg, Task<TResult>> queryFn,
        QueryOptions<TResult>? options = null
    ) : this(new QueryCache<TArg, TResult>(queryFn, TimeSpan.Zero), options) { }

    public void Trigger(TArg arg) => _ = TriggerAsync(arg);

    public async Task<TResult> TriggerAsync(TArg arg)
    {
        // TODO: Re-use when arguments unchanged?
        var query = _cache.AddUncached(arg);
        _currentQuery?.RemoveObserver(this);
        query.AddObserver(this);
        _currentQuery = query;
        return await query.RefetchAsync().ConfigureAwait(false);
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

    /// <summary>
    /// Runs the original query function once, completely bypassing caching and other extra behaviour
    /// </summary>
    /// <param name="arg">The argument passed to the query function</param>
    /// <returns>The value returned by the query function</returns>
    public Task<TResult> Invoke(TArg arg)
    {
        return _cache.QueryFn.Invoke(arg);
    }

    public void Cancel() => throw new NotImplementedException();

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

public class Mutation<TArg> : Mutation<TArg, Unit>
{
    public Mutation(
        Func<TArg, CancellationToken, Task> mutationFn,
        QueryOptions<Unit>? endpointOptions = null
    ) : base(
        async (arg) =>
        {
            await mutationFn(arg, default);
            return new Unit();
        },
        endpointOptions)
    {
    }

    public Mutation(
        QueryCache<TArg, Unit> cache,
        QueryOptions<Unit>? options = null
    ) : base(cache, options)
    {
    }
}
