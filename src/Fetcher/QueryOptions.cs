namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Text;

public class QueryOptions<TResult>
{
    public TResult? InitialData { get; init; }
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);
}

public class QueryObserverOptions<TResult> : QueryOptions<TResult>
{
    public TimeSpan StaleTime { get; init; } = TimeSpan.Zero;
    public Action<TResult?>? OnSuccess { get; init; } // TODO: Handle OnSuccess for unwatched queries?
    public Action<Exception>? OnFailure { get; init; }
}
