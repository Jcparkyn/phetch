namespace Phetch.Core;

using System;
using System.Threading.Tasks;

/// <summary>
/// Defines an "endpoint" that represents a single query function, usually for a specific HTTP endpoint.
/// </summary>
/// <typeparam name="TArg">
/// The type of the argument passed to the query function. To use query functions with multiple
/// arguments, wrap them in a tuple.
/// </typeparam>
/// <typeparam name="TResult">The return type from the query function</typeparam>
/// <remarks>
/// This is the recommended way to use queries in most cases, and serves as a convenient way to create <see
/// cref="Query{TArg, TResult}"/> instances that share the same cache.
/// </remarks>
public class QueryEndpoint<TArg, TResult>
{
    protected readonly QueryCache<TArg, TResult> Cache;

    /// <summary>
    /// Creates a new query endpoint with a given query function. In most cases, the query function
    /// will be a call to an HTTP endpoint, but it can be any async function.
    /// </summary>
    public QueryEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        QueryEndpointOptions<TResult>? options = null)
    {
        options ??= new();
        Cache = new(queryFn, options);
    }

    /// <summary>
    /// Creates a new <see cref="Query{TArg, TResult}"/> object, which can be used to make queries
    /// to this endpoint.
    /// </summary>
    /// <returns>A new <see cref="Query{TArg, TResult}"/> object which shares the same cache as other queries from this endpoint.</returns>
    /// <param name="options">Additional options to use when querying</param>
    public Query<TArg, TResult> Use(QueryOptions<TResult>? options = null)
    {
        return new Query<TArg, TResult>(Cache, options);
    }

    /// <summary>
    /// Invalidates all cached return values from this endpoint. Any components using them will
    /// automatically re-fetch their data.
    /// </summary>
    public void InvalidateAll()
    {
        Cache.InvalidateAll();
    }

    /// <summary>
    /// Invalidates a specific value in the cache, based on its query argument.
    /// </summary>
    /// <param name="arg">The query argument to invalidate</param>
    /// <remarks>
    /// <para/>
    /// This should be preferred over <see cref="InvalidateWhere"/>, because it is more efficient.
    /// <para/>
    /// If no queries are using the provided query argument, this does nothing.
    /// </remarks>
    public void Invalidate(TArg arg)
    {
        Cache.Invalidate(arg);
    }

    /// <summary>
    /// Invalidates all cache entries that match the given predicate.
    /// </summary>
    /// <param name="predicate">
    /// The function to use when deciding which entries to invalidate. The arguments to this
    /// function are the query arg, and the query object itself. This should return <c>true</c> for
    /// entries that should be invalidated, or false otherwise.
    /// </param>
    public void InvalidateWhere(Func<TArg, FixedQuery<TResult>, bool> predicate)
    {
        Cache.InvalidateWhere(predicate);
    }

    /// <inheritdoc cref="QueryCache{TArg, TResult}.UpdateQueryData(TArg, TResult)"/>
    public bool UpdateQueryData(TArg arg, TResult resultData) => Cache.UpdateQueryData(arg, resultData);

    /// <summary>
    /// Runs the original query function once, completely bypassing caching and other extra behaviour
    /// </summary>
    /// <param name="arg">The argument passed to the query function</param>
    /// <returns>The value returned by the query function</returns>
    public Task<TResult> Invoke(TArg arg)
    {
        return Cache.QueryFn.Invoke(arg);
    }
}

/// <summary>
/// An alternate version of <see cref="QueryEndpoint{TArg, TResult}"/> for queries that have no parameters.
/// </summary>
public class QueryEndpoint<TResult> : QueryEndpoint<Unit, TResult>
{
    public QueryEndpoint(
        Func<Task<TResult>> queryFn,
        QueryEndpointOptions<TResult>? options = null
    ) : base(_ => queryFn(), options)
    {
    }

    public new Query<TResult> Use(QueryOptions<TResult>? options = null)
    {
        return new Query<TResult>(Cache, options);
    }
}
