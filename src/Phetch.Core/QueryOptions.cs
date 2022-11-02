namespace Phetch.Core;

using System;

/// <summary>
/// Options that are passed to an Endpoint.
/// </summary>
public sealed record EndpointOptions<TArg, TResult>
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
    /// Default stale time to be used if not supplied when using the endpoint. This defaults to
    /// zero, so queries are considered stale as soon as they finish fetching.
    /// </summary>
    /// <remarks>This can be overriden by <see cref="QueryOptions{TArg, TResult}.StaleTime"/></remarks>
    public TimeSpan DefaultStaleTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QuerySuccessEventArgs<TArg, TResult>>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureEventArgs<TArg>>? OnFailure { get; init; }

    /// <summary>
    /// An optional object to control whether and how the query function is retried if it fails. If
    /// left null, the query will not be retried when it fails.
    /// </summary>
    /// <remarks>
    /// <example><b>Example:</b>
    /// <code>
    /// var endpoint = new Endpoint&lt;int, string&gt;(..., new() {
    ///     RetryHandler = RetryHandler.Simple(3);
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public IRetryHandler? RetryHandler { get; init; }
}

/// <summary>
/// Options that are passed when creating a <see cref="Query{TArg, TResult}"/> or calling <see
/// cref="Endpoint{TArg, TResult}.Use"/>.
/// </summary>
public sealed record QueryOptions<TArg, TResult>
{
    internal static QueryOptions<TArg, TResult> Default { get; } = new();

    /// <summary>
    /// The amount of time until this query is considered "stale". If not set, the <see
    /// cref="EndpointOptions{TArg, TResult}.DefaultStaleTime"/> (zero by default) will be used instead.
    /// <para/>
    /// If a cached query is used <b>before</b> it becomes stale, the component will receive the
    /// cached result and won't re-fetch the data. If a cached query is used <b>after</b> it becomes
    /// stale, the cached data will be used initially, but new data will be re-fetched in the
    /// background automatically.
    /// </summary>
    public TimeSpan? StaleTime { get; init; }

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QuerySuccessEventArgs<TArg, TResult>>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureEventArgs<TArg>>? OnFailure { get; init; }

    /// <summary>
    /// If set, overrides the default RetryHandler for the endpoint.
    /// <para/>
    /// To remove the endpoint's retry handler if it has one, set this to <see cref="RetryHandler.None"/>.
    /// </summary>
    public IRetryHandler? RetryHandler { get; init; }
}

/// <summary>
/// Object containing information about a succeeded query.
/// </summary>
public sealed class QuerySuccessEventArgs<TArg, TResult> : EventArgs
{
    /// <summary>
    /// The original argument passed to the query.
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// The value returned by the query.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Creates a new QuerySuccessEventArgs
    /// </summary>
    public QuerySuccessEventArgs(TArg arg, TResult result)
    {
        Arg = arg;
        Result = result;
    }
}

/// <summary>
/// Object containing information about a succeeded query.
/// </summary>
public sealed class QueryFailureEventArgs<TArg> : EventArgs
{
    /// <summary>
    /// The original argument passed to the query.
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// The exception thrown by the query.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Creates a new QueryFailureEventArgs
    /// </summary>
    public QueryFailureEventArgs(TArg arg, Exception exception)
    {
        Arg = arg;
        Exception = exception;
    }
}
