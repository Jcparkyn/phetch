namespace Phetch.Core;

using System;
using System.Threading;
using System.Threading.Tasks;

public class MutationEndpoint<TArg, TResult>
{
    protected readonly QueryCache<TArg, TResult> Cache;
    protected readonly MutationEndpointOptions<TResult>? Options;

    /// <summary>
    /// Creates a new mutation endpoint with a given query function. In most cases, the query function
    /// will be a call to an HTTP endpoint, but it can be any async function.
    /// </summary>
    public MutationEndpoint(
        Func<TArg, CancellationToken, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
    {
        Cache = new(queryFn, (options ?? new()).CacheTime);
    }

    /// <inheritdoc cref="MutationEndpoint{TArg, TResult}.MutationEndpoint(Func{TArg, CancellationToken, Task{TResult}}, MutationEndpointOptions{TResult}?)"/>
    public MutationEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
        : this((arg, _) => queryFn(arg), options)
    { }

    public Mutation<TArg, TResult> Use(QueryOptions<TResult>? options = null)
    {
        return new Mutation<TArg, TResult>(Cache, options);
    }
}

public class MutationEndpoint<TArg> : MutationEndpoint<TArg, Unit>
{
    public MutationEndpoint(
        Func<TArg, CancellationToken, Task> queryFn,
        MutationEndpointOptions<Unit>? options = null
    ) : base(
        async (arg, token) =>
        {
            await queryFn(arg, token);
            return default;
        },
        options
    )
    { }

    public MutationEndpoint(
        Func<TArg, Task> queryFn,
        MutationEndpointOptions<Unit>? options = null
    ) : base(
        async (arg, _) =>
        {
            await queryFn(arg);
            return default;
        },
        options
    )
    { }

    public new Mutation<TArg> Use(QueryOptions<Unit>? options = null) => new(Cache, options);
}
