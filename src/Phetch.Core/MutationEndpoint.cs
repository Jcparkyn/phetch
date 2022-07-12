namespace Phetch.Core;

using System;
using System.Threading.Tasks;

public class MutationEndpoint<TArg, TResult>
{
    protected readonly Func<TArg, Task<TResult>> QueryFn;
    protected readonly MutationEndpointOptions<TResult>? Options;

    public MutationEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
    {
        QueryFn = queryFn;
        Options = options;
    }

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
        Func<TArg, Task> queryFn,
        MutationEndpointOptions<Unit>? options = null
    ) : base(
        async arg =>
        {
            await queryFn(arg);
            return default;
        },
        options
    )
    { }

    public new Mutation<TArg> Use() => new(QueryFn, Options);
}
