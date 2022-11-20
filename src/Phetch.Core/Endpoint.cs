namespace Phetch.Core;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines an "endpoint" that represents a single query function, usually for a specific HTTP endpoint.
/// </summary>
/// <typeparam name="TArg">
/// The type of the argument passed to the query function. To use query functions with multiple
/// arguments, wrap them in a tuple.
/// </typeparam>
/// <typeparam name="TResult">The return type from the query function</typeparam>
/// <remarks>
/// This is the recommended way to use queries in most cases, and serves as a convenient way to create <see
/// cref="Query{TArg, TResult}"/> instances that share the same cache.
/// </remarks>
public class Endpoint<TArg, TResult>
{
    /// <summary>
    /// Options for this endpoint.
    /// </summary>
    public EndpointOptions<TArg, TResult> Options { get; }

    internal QueryCache<TArg, TResult> Cache { get; }

    /// <summary>
    /// Creates a new query endpoint with a given query function. In most cases, the query function
    /// will be a call to an HTTP endpoint, but it can be any async function.
    /// </summary>
    public Endpoint(
        Func<TArg, CancellationToken, Task<TResult>> queryFn,
        EndpointOptions<TArg, TResult>? options = null)
    {
        _ = queryFn ?? throw new ArgumentNullException(nameof(queryFn));
        Options = options ?? EndpointOptions<TArg, TResult>.Default;
        Cache = new(queryFn, Options);
    }

    /// <summary>
    /// Creates a new query endpoint with a given query function. In most cases, the query function
    /// will be a call to an HTTP endpoint, but it can be any async function.
    /// </summary>
    public Endpoint(
        Func<TArg, Task<TResult>> queryFn,
        EndpointOptions<TArg, TResult>? options = null)
        : this((arg, _) => queryFn(arg), options)
    { }

    /// <summary>
    /// Creates a new <see cref="Query{TArg, TResult}"/> object, which can be used to make queries
    /// to this endpoint.
    /// </summary>
    /// <returns>A new <see cref="Query{TArg, TResult}"/> object which shares the same cache as other queries from this endpoint.</returns>
    /// <param name="options">Additional options to use when querying</param>
    public Query<TArg, TResult> Use(QueryOptions<TArg, TResult>? options = null)
    {
        return new Query<TArg, TResult>(Cache, options, Options);
    }

    /// <summary>
    /// Invalidates all cached return values from this endpoint. Any components using them will
    /// automatically re-fetch their data.
    /// </summary>
    public void InvalidateAll()
    {
        Cache.InvalidateAll();
    }

    /// <summary>
    /// Invalidates a specific value in the cache, based on its query argument.
    /// </summary>
    /// <param name="arg">The query argument to invalidate</param>
    /// <remarks>
    /// <para/>
    /// If no queries are using the provided query argument, this does nothing.
    /// </remarks>
    public void Invalidate(TArg arg)
    {
        Cache.Invalidate(arg);
    }

    /// <summary>
    /// Invalidates all cache entries that match the given predicate.
    /// </summary>
    /// <param name="predicate">
    /// The function to use when deciding which entries to invalidate, based on the cached query.
    /// This should return <c>true</c> for entries that should be invalidated, or false otherwise.
    /// </param>
    public void InvalidateWhere(Func<FixedQuery<TArg, TResult>, bool> predicate)
    {
        Cache.InvalidateWhere(predicate ?? throw new ArgumentNullException(nameof(predicate)));
    }

    /// <summary>
    /// Updates the response data for a given query. If no cache entry exists for this arg and
    /// <paramref name="addIfNotExists"/> is <c>true</c>, a new one will be created.
    /// </summary>
    /// <param name="arg">The query argument of the query to be updated.</param>
    /// <param name="resultData">The new data to set on the query.</param>
    /// <param name="addIfNotExists"></param>
    public void UpdateQueryData(TArg arg, TResult resultData, bool addIfNotExists = false)
        => Cache.UpdateQueryData(arg, resultData, addIfNotExists);

    /// <summary>
    /// Updates the response data for a given query. If no cache entry exists for this arg and
    /// <paramref name="addIfNotExists"/> is <c>true</c>, a new one will be created.
    /// </summary>
    /// <remarks>
    /// Note: It is not guaranteed that the the query object passed to <paramref
    ///       name="dataSelector"/> will have succeeded and/or have data available.
    /// </remarks>
    /// <param name="arg">The query argument of the query to be updated.</param>
    /// <param name="dataSelector">
    /// A function to select the new data for the query, based on the existing cached query.
    /// </param>
    /// <param name="addIfNotExists">
    /// If <c>true</c> and there is no cache entry for the given <paramref name="arg"/>, a new query
    /// will be added to the cache.
    /// </param>
    public void UpdateQueryData(TArg arg, Func<FixedQuery<TArg, TResult>, TResult> dataSelector, bool addIfNotExists = false)
        => Cache.UpdateQueryData(arg, dataSelector ?? throw new ArgumentNullException(nameof(dataSelector)), addIfNotExists);

    /// <summary>
    /// Begins running the query in the background for the specified query argument, so that the
    /// result can be cached and used immediately when it is needed.
    /// <para/>
    /// If the specified query argument already exists in the cache and was not an error, this does nothing.
    /// </summary>
    public async Task<TResult> PrefetchAsync(TArg arg)
    {
        var query = Cache.GetOrAdd(arg);
        if (query.Status == QueryStatus.Idle || query.Status == QueryStatus.Error)
        {
            return await query.RefetchAsync(retryHandler: null);
        }
        Debug.Assert(query.LastInvokation is not null, "query should always be invoked before this point.");
        return await query.LastInvokation;
    }

    /// <inheritdoc cref="PrefetchAsync(TArg)"/>
    public void Prefetch(TArg arg) => _ = PrefetchAsync(arg);

    /// <summary>
    /// Runs the original query function once, completely bypassing caching and other extra behaviour
    /// </summary>
    /// <param name="arg">The argument passed to the query function</param>
    /// <param name="ct">An optional cancellation token</param>
    /// <returns>The value returned by the query function</returns>
    public Task<TResult> Invoke(TArg arg, CancellationToken ct = default)
    {
        return Cache.QueryFn.Invoke(arg, ct);
    }

    /// <summary>
    /// Gets the cached query instance for the given argument if it exists in the cache, otherwise <c>null</c>.
    /// </summary>
    /// <remarks>
    /// This does not return queries created by <see cref="Query{TArg, TResult}.Trigger(TArg)"/>.
    /// </remarks>
    public FixedQuery<TArg, TResult>? GetCachedQuery(TArg arg) => Cache.GetCachedQuery(arg);

    /// <summary>
    /// Similar to <see cref="GetCachedQuery(TArg)"/>, but looks up a query by it's key directly.
    /// This is only useful when using <see cref="EndpointOptions{TArg, TResult}.KeySelector"/>,
    /// because otherwise the key and arguments are equivalent.
    /// </summary>
    public FixedQuery<TArg, TResult>? GetCachedQueryByKey(object? key) => Cache.GetCachedQueryByKey(key);

    /// <summary>
    /// Attempts to retrieve a cached result for the given query argument.
    /// </summary>
    /// <param name="arg">The query argument</param>
    /// <param name="result">
    /// The cached data for the given query argument, or <c>default</c> if the data wasn't in the cache.
    /// </param>
    /// <returns>True if the data existed in the cache.</returns>
    public bool TryGetCachedResult(TArg arg, out TResult result)
    {
        var query = Cache.GetCachedQuery(arg);
        if (query is not null && query.Status == QueryStatus.Success)
        {
            result = query.Data!;
            return true;
        }
        result = default!;
        return false;
    }
}

/// <summary>
/// An alternate version of <see cref="Endpoint{TArg, TResult}"/> for queries that have no parameters.
/// </summary>
public sealed class ParameterlessEndpoint<TResult> : Endpoint<Unit, TResult>
{
    /// <summary>
    /// Creates a new Endpoint from a query function with no parameters.
    /// </summary>
    public ParameterlessEndpoint(
        Func<CancellationToken, Task<TResult>> queryFn,
        EndpointOptions<Unit, TResult>? options = null
    ) : base((_, ct) => queryFn(ct), options)
    { }

    /// <summary>
    /// Creates a new Endpoint from a query function with no parameters and no CancellationToken.
    /// </summary>
    public ParameterlessEndpoint(
        Func<Task<TResult>> queryFn,
        EndpointOptions<Unit, TResult>? options = null
    ) : base((_, _) => queryFn(), options)
    { }

    /// <inheritdoc cref="Endpoint{TArg, TResult}.Use"/>
    public new Query<TResult> Use(QueryOptions<Unit, TResult>? options = null) =>
        new(Cache, options, Options);
}

/// <summary>
/// An alternate version of <see cref="Endpoint{TArg, TResult}"/> for queries that have no return value.
/// </summary>
public sealed class MutationEndpoint<TArg> : Endpoint<TArg, Unit>
{
    /// <summary>
    /// Creates a new Endpoint from a query function with no return value.
    /// </summary>
    public MutationEndpoint(
        Func<TArg, CancellationToken, Task> queryFn,
        EndpointOptions<TArg, Unit>? options = null
    ) : base(
        async (arg, token) =>
        {
            await queryFn(arg, token);
            return default;
        },
        options
    )
    { }

    /// <summary>
    /// Creates a new Endpoint from a query function with no return value and no CancellationToken.
    /// </summary>
    public MutationEndpoint(
        Func<TArg, Task> queryFn,
        EndpointOptions<TArg, Unit>? options = null
    ) : base(
        async (arg, _) =>
        {
            await queryFn(arg);
            return default;
        },
        options
    )
    { }

    /// <inheritdoc cref="Endpoint{TArg, TResult}.Use"/>
    public new Mutation<TArg> Use(QueryOptions<TArg, Unit>? options = null) =>
        new(Cache, options, Options);
}
