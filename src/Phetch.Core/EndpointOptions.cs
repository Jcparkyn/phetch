namespace Phetch.Core;

using System;

/// <summary>
/// A re-usable version of <see cref="EndpointOptions{TArg, TResult}"/> without type arguments, which can
/// be used to share endpoint settings across multiple endpoints.
/// </summary>
/// <remarks>
/// This can be customized for each endpoint using the <see cref="EndpointOptions{TArg, TResult}"/> constructor.
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
    /// When set to <see cref="TimeSpan.MaxValue"/>, queries will never be removed from the cache.
    /// </remarks>
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default stale time to be used if not supplied when using the endpoint. This defaults to
    /// zero, so queries are considered stale as soon as they finish fetching.
    /// <para/>
    /// When set to <see cref="TimeSpan.MaxValue"/>, queries will never be considered stale (unless
    /// they are manually invalidated).
    /// </summary>
    /// <remarks>This can be overridden by <see cref="QueryOptions{TArg, TResult}.StaleTime"/></remarks>
    public TimeSpan DefaultStaleTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// A function that gets run whenever this query succeeds.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="QueryOptions.OnSuccess"/>, this will be called even if the query is not
    /// being observed.
    /// </remarks>
    public Action<EventArgs>? OnSuccess { get; init; }

    /// <summary>
    /// A function that gets run whenever this query fails.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="QueryOptions.OnFailure"/>, this will be called even if the query is not
    /// being observed.
    /// </remarks>
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
    /// A function that can be used to override the default behavior for determining which query
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
