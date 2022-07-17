namespace Phetch.Core;

using System;
using System.Threading;
using System.Threading.Tasks;

public class MutationEndpoint<TArg, TResult>
{
    protected readonly Func<TArg, CancellationToken, Task<TResult>> QueryFn;
    protected readonly MutationEndpointOptions<TResult>? Options;

    /// <summary>
    /// Creates a new mutation endpoint with a given query function. In most cases, the query function
    /// will be a call to an HTTP endpoint, but it can be any async function.
    /// </summary>
    public MutationEndpoint(
        Func<TArg, CancellationToken, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
    {
        QueryFn = queryFn;
        Options = options;
    }

    /// <inheritdoc cref="MutationEndpoint{TArg, TResult}.MutationEndpoint(Func{TArg, CancellationToken, Task{TResult}}, MutationEndpointOptions{TResult}?)"/>
    public MutationEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
        : this((arg, _) => queryFn(arg), options)
    { }

    public Mutation<TArg, TResult> Use()
    {
        return new Mutation<TArg, TResult>(
            QueryFn,
            endpointOptions: Options);
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

    public new Mutation<TArg> Use() => new(QueryFn, Options);
}
