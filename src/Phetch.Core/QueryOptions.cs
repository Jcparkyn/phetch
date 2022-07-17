namespace Phetch.Core;

using System;

public class QueryEndpointOptions<TResult>
{
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
}

public class QueryOptions<TArg, TResult>
{
    /// <summary>
    /// The amount of time until this query is considered "stale".
    /// </summary>
    /// <remarks>
    /// If a cached query is used <b>before</b> it becomes stale, the component will recieve the
    /// cached result and won't re-fetch the data. If a cached query is used <b>after</b> it becomes
    /// stale, the cached data will be used initially, but new data will be re-fetched in the
    /// background automatically.
    /// </remarks>
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// </summary>
    /// <remarks>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </remarks>
    public Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// </summary>
    /// <remarks>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </remarks>
    public Action<QueryFailureContext<TArg>>? OnFailure { get; init; }
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

