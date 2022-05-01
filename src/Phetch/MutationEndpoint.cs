﻿namespace Phetch;

using System;
using System.Threading.Tasks;

public class MutationEndpoint<TArg, TResult>
{
    private readonly Func<TArg, Task<TResult>> _queryFn;
    private readonly MutationEndpointOptions<TResult>? _options;

    public MutationEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        MutationEndpointOptions<TResult>? options = null)
    {
        _queryFn = queryFn;
        _options = options;
    }

    public Mutation<TArg, TResult> Use()
    {
        return new Mutation<TArg, TResult>(
            _queryFn,
            endpointOptions: _options);
    }
}