namespace Fetcher;

using System;

public class QueryMethodOptions<TResult>
{
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);
    //public QueryObserverOptions<TResult>? DefaultObserverOptions { get; set; }
}

public class QueryObserverOptions<TResult>
{
    //public TResult? PlaceholderData { get; init; }
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;
    public Action<TResult?>? OnSuccess { get; init; }
    public Action<Exception>? OnFailure { get; init; }
}
