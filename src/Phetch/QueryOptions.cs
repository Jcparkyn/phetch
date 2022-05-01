namespace Phetch;

using System;

public class QueryEndpointOptions<TResult>
{
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);
    //public QueryObserverOptions<TResult>? DefaultObserverOptions { get; set; }
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

public class MutationOptions<TResult>
{
    public Action<TResult>? OnSuccess { get; init; }
    public Action<Exception>? OnFailure { get; init; }
}
