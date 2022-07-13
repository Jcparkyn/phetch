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

public class QueryOptions<TResult>
{
    //public TResult? PlaceholderData { get; init; }
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;
    public Action<TResult?>? OnSuccess { get; init; }
    public Action<Exception>? OnFailure { get; init; }
}

public class MutationEndpointOptions<TResult>
{
    public Action<TResult>? OnSuccess { get; init; }
    public Action<Exception>? OnFailure { get; init; }
}
