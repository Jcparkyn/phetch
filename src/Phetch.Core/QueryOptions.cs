namespace Phetch.Core;

using System;

/// <summary>
/// Options that are passed to an Endpoint.
/// </summary>
public record EndpointOptions<TArg, TResult>
{
    internal static EndpointOptions<TArg, TResult> Default { get; } = new();

    /// <summary>
    /// The amount of time to store query results in the cache after they stop being used.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="TimeSpan.Zero"/>, queries will be removed from the cache as soon as
    /// they have no observers.
    /// <para/>
    /// When set to a negative value, queries will never be removed from the cache.
    /// </remarks>
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; init; } = null;

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureContext<TArg>>? OnFailure { get; init; } = null;
}

/// <summary>
/// Options that are passed when creating a <see cref="Query{TArg, TResult}"/> or calling <see
/// cref="Endpoint{TArg, TResult}.Use"/>.
/// </summary>
public record QueryOptions<TArg, TResult>
{
    internal static QueryOptions<TArg, TResult> Default { get; } = new();

    /// <summary>
    /// The amount of time until this query is considered "stale". This defaults to zero, so queries are
    /// considered stale as soon as they finish fetching.
    /// <para/>
    /// If a cached query is used <b>before</b> it becomes stale, the component will recieve the cached
    /// result and won't re-fetch the data. If a cached query is used <b>after</b> it becomes stale, the
    /// cached data will be used initially, but new data will be re-fetched in the background automatically.
    /// </summary>
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; init; } = null;

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureContext<TArg>>? OnFailure { get; init; } = null;
}

/// <summary>
/// Object containing information about a succeeded query
/// </summary>
/// <param name="Arg">The original argument passed to the query</param>
/// <param name="Result">The value returned by the query</param>
public record QuerySuccessContext<TArg, TResult>(
    TArg Arg,
    TResult Result);

/// <summary>
/// Object containing information about a faild query
/// </summary>
/// <param name="Arg">The original argument passed to the query</param>
/// <param name="Exception">The exception thrown by the query</param>
public record QueryFailureContext<TArg>(
    TArg Arg,
    Exception Exception);

