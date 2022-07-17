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
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;
    public Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; init; }
    public Action<QueryFailureContext<TArg>>? OnFailure { get; init; }
}

public record QuerySuccessContext<TArg, TResult>(
    TArg Arg,
    TResult Result);

public record QueryFailureContext<TArg>(
    TArg Arg,
    Exception Exception);

