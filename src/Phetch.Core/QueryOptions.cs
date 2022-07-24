namespace Phetch.Core;

using System;

public record QueryEndpointOptions<TResult>
{
    public static QueryEndpointOptions<TResult> Default { get; } = new();

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

/// <summary>
/// </summary>
/// <typeparam name="TArg"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <param name="StaleTime">
/// The amount of time until this query is considered "stale". This defaults to zero, so queries are
/// considered stale as soon as they finish fetching.
/// <para/>
/// If a cached query is used <b>before</b> it becomes stale, the component will recieve the cached
/// result and won't re-fetch the data. If a cached query is used <b>after</b> it becomes stale, the
/// cached data will be used initially, but new data will be re-fetched in the background automatically.
/// </param>
/// <param name="OnSuccess">
/// A function that gets run whenever this query succeeds.
/// <para/>
/// To avoid a race condition when multiple queries return in a different order than they were
/// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
/// </param>
/// <param name="OnFailure">
/// A function that gets run whenever this query fails.
/// <para/>
/// To avoid a race condition when multiple queries return in a different order than they were
/// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
/// </param>
public record QueryOptions<TArg, TResult>(
    TimeSpan StaleTime = default,
    Action<QuerySuccessContext<TArg, TResult>>? OnSuccess = null,
    Action<QueryFailureContext<TArg>>? OnFailure = null
)
{
    public static QueryOptions<TArg, TResult> Default { get; } = new();
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

