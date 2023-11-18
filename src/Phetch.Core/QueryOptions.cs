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
    /// <para/>
    /// When set to <see cref="TimeSpan.MaxValue"/>, queries will never be considered stale (unless
    /// they are manually invalidated).
    /// </summary>
    public TimeSpan? StaleTime { get; init; }

    /// <summary>
    /// A function that gets run when this query succeeds.
    /// </summary>
    /// <remarks>
    /// This is only called when the query is currently being observed, which means:
    /// <list type="number">
    /// <item>
    /// If a query arg is changed while it is still fetching a previous arg, <c>OnSuccess</c> will
    /// only be called for the latest arg.
    /// </item>
    /// <item>
    /// If the observing component is unmounted before the query finishes, <c>OnSuccess</c> will not
    /// be called.
    /// </item>
    /// </list>
    /// If you want to call a function <b>every</b> time a query succeeds, use <see
    /// cref="EndpointOptions.OnSuccess"/> when creating an endpoint.
    /// </remarks>
    public Action<EventArgs>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run when this query fails.
    /// </summary>
    /// <remarks>
    /// This is only called when the query is currently being observed, which means:
    /// <list type="number">
    /// <item>
    /// If a query arg is changed while it is still fetching a previous arg, <c>OnFailure</c> will
    /// only be called for the latest arg.
    /// </item>
    /// <item>
    /// If the observing component is unmounted before the query finishes, <c>OnFailure</c> will not
    /// be called.
    /// </item>
    /// </list>
    /// If you want to call a function <b>every</b> time a query fails, use <see
    /// cref="EndpointOptions.OnFailure"/> when creating an endpoint.
    /// </remarks>
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

    /// <inheritdoc cref="QueryOptions.OnSuccess"/>
    public Action<QuerySuccessEventArgs<TArg, TResult>>? OnSuccess { get; init; }

    /// <inheritdoc cref="QueryOptions.OnFailure"/>
    public Action<QueryFailureEventArgs<TArg>>? OnFailure { get; init; }

    /// <summary>
    /// If set, overrides the default RetryHandler for the endpoint.
    /// <para/>
    /// To remove the endpoint's retry handler if it has one, set this to <see cref="RetryHandler.None"/>.
    /// </summary>
    public IRetryHandler? RetryHandler { get; init; }
}
