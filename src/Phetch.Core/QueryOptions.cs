namespace Phetch.Core;

using System;

/// <summary>
/// A re-usable version of <see cref="EndpointOptions{TArg, TResult}"/> without type arguments, which can
/// be used to share endpoint settings across multiple endpoints.
/// </summary>
/// <remarks>
/// This can be customised for each endpoint using the <see cref="EndpointOptions{TArg, TResult}"/> constructor.
/// <para/>
/// <code>
/// var defaultEndpointOptions = new EndpointOptions
/// {
///     RetryHandler = RetryHandler.Simple(3)
/// };
/// var endpoint = new Endpoint&lt;int, string&gt;(..., new(defaultEndpointOptions)
/// {
///     RetryHandler = RetryHandler.None,
///     CacheTime = TimeSpan.Zero,
/// });
/// </code>
/// </remarks>
public sealed record EndpointOptions
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
    public Action<EventArgs>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// <para/>
    /// To avoid a race condition when multiple queries return in a different order than they were
    /// started, this only gets called if the data is "current" (i.e., no newer queries have already returned).
    /// </summary>
    public Action<QueryFailureEventArgs>? OnFailure { get; init; }

    /// <summary>
    /// An optional object to control whether and how the query function is retried if it fails. If
    /// left null, the query will not be retried when it fails.
    /// </summary>
    /// <remarks>
    /// <example><b>Example:</b>
    /// <code>
    /// var endpoint = new Endpoint&lt;int, string&gt;(..., new() {
    ///     RetryHandler = RetryHandler.Simple(3)
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public IRetryHandler? RetryHandler { get; init; }
}

/// <summary>
/// Options that are passed to an Endpoint.
/// </summary>
public sealed record EndpointOptions<TArg, TResult>()
{
    /// <summary>
    /// Creates a strongly-typed EndpointOptions&lt;TArg, TResult&gt; from an EndpointOptions instance.
    /// </summary>
    public EndpointOptions(EndpointOptions original) : this()
    {
        _ = original ?? throw new ArgumentNullException(nameof(original));
        CacheTime = original.CacheTime;
        DefaultStaleTime = original.DefaultStaleTime;
        OnSuccess = original.OnSuccess;
        OnFailure = original.OnFailure;
        RetryHandler = original.RetryHandler;
    }

    private static EndpointOptions<TArg, TResult>? s_default;

    /// <summary>
    /// An instance of <see cref="EndpointOptions{TArg, TResult}"/> with default values.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public static EndpointOptions<TArg, TResult> Default => s_default ??= new();

    /// <inheritdoc cref="EndpointOptions.CacheTime"/>
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);

    /// <inheritdoc cref="EndpointOptions.DefaultStaleTime"/>
    public TimeSpan DefaultStaleTime { get; init; } = TimeSpan.Zero;

    /// <inheritdoc cref="EndpointOptions.OnSuccess"/>
    public Action<QuerySuccessEventArgs<TArg, TResult>>? OnSuccess { get; init; }

    /// <inheritdoc cref="EndpointOptions.OnFailure"/>
    public Action<QueryFailureEventArgs<TArg>>? OnFailure { get; init; }

    /// <inheritdoc cref="EndpointOptions.RetryHandler"/>
    public IRetryHandler? RetryHandler { get; init; }

    /// <summary>
    /// A function that can be used to override the default behaviour for determining which query
    /// arguments are the same. The object returned by this function will be used as the dictionary
    /// keys for the query cache. This is useful if your query argument type is not suitable to use
    /// a dictionary key, because it doesn't implement GetHashCode and Equals.
    /// <para/>
    /// If not provided, the query arguments are used as dictionary keys directly.
    /// </summary>
    /// <remarks>
    /// <example> In many cases, the best way to use this is by returning a tuple of all relevant fields:
    /// <code>
    ///KeySelector = arg => (arg.Id, arg.Name)
    /// </code>
    /// </example>
    /// </remarks>
    public Func<TArg, object>? KeySelector { get; set; }

    /// <summary>
    /// Converts an untyped <see cref="EndpointOptions"/> instance into an <see cref="Endpoint{TArg, TResult}"/>.
    /// </summary>
    /// <param name="original"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2225:Operator overloads have named alternates",
        Justification = "Constructor is clearer than ToEndpointOptions method.")]
    public static implicit operator EndpointOptions<TArg, TResult>(EndpointOptions original) => new(original);
}

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
/// Object containing information about a succeeded query, without type information.
/// </summary>
public abstract class QueryFailureEventArgs : EventArgs
{
    /// <summary>
    /// The exception thrown by the query.
    /// </summary>
    public Exception Exception { get; }

    internal QueryFailureEventArgs(Exception exception)
    {
        Exception = exception;
    }
}

/// <summary>
/// Object containing information about a succeeded query.
/// </summary>
public sealed class QueryFailureEventArgs<TArg> : QueryFailureEventArgs
{
    /// <summary>
    /// The original argument passed to the query.
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// Creates a new QueryFailureEventArgs
    /// </summary>
    public QueryFailureEventArgs(TArg arg, Exception exception) : base(exception)
    {
        Arg = arg;
    }
}
