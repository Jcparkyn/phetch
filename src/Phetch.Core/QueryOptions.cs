namespace Phetch.Core;

using System;

/// <summary>
/// A re-usable version of <see cref="QueryOptions{TArg, TResult}"/> without type arguments, which
/// can be used to share query settings across multiple calls to <see cref="Endpoint{TArg, TResult}.Use">endpoint.Use(options)</see>.
/// </summary>
public sealed record QueryOptions()
{
    private static QueryOptions? s_default;

    /// <summary>
    /// An instance of <see cref="QueryOptions{TArg, TResult}"/> with default values.
    /// </summary>
    public static QueryOptions Default => s_default ??= new();

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
    public Action<EventArgs>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureEventArgs>? OnFailure { get; init; }

    /// <summary>
    /// If set, overrides the default RetryHandler for the endpoint.
    /// <para/>
    /// To remove the endpoint's retry handler if it has one, set this to <see cref="RetryHandler.None"/>.
    /// </summary>
    public IRetryHandler? RetryHandler { get; init; }
}

/// <summary>
/// Options that are passed when creating a <see cref="Query{TArg, TResult}"/> or calling <see
/// cref="Endpoint{TArg, TResult}.Use"/>.
/// </summary>
public sealed record QueryOptions<TArg, TResult>()
{
    /// <summary>
    /// Creates a strongly-typed QueryOptions&lt;TArg, TResult&gt; from a QueryOptions instance.
    /// </summary>
    public QueryOptions(QueryOptions original) : this()
    {
        _ = original ?? throw new ArgumentNullException(nameof(original));
        StaleTime = original.StaleTime;
        OnSuccess = original.OnSuccess;
        OnFailure = original.OnFailure;
        RetryHandler = original.RetryHandler;
    }

    private static QueryOptions<TArg, TResult>? s_default;

    /// <summary>
    /// An instance of <see cref="QueryOptions{TArg, TResult}"/> with default values.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public static QueryOptions<TArg, TResult> Default => s_default ??= new();

    /// <inheritdoc cref="QueryOptions.StaleTime"/>
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
