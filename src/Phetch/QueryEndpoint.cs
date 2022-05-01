namespace Phetch;

using System;
using System.Threading.Tasks;

public class QueryEndpoint<TArg, TResult>
{
    protected readonly QueryCache<TArg, TResult> Cache;

    public QueryEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        QueryEndpointOptions<TResult>? options = null)
    {
        options ??= new();
        Cache = new(queryFn, options);
    }

    public Query<TArg, TResult> Use(QueryOptions<TResult> options)
    {
        return new Query<TArg, TResult>(Cache, options);
    }

    public Query<TArg, TResult> Use() => Use(new());

    public void InvalidateAll()
    {
        Cache.InvalidateAll();
    }

    public void Invalidate(TArg arg)
    {
        Cache.Invalidate(arg);
    }

    public void InvalidateWhere(Func<TArg, FixedQuery<TResult>, bool> predicate)
    {
        Cache.InvalidateWhere(predicate);
    }

    /// <inheritdoc cref="QueryCache{TArg, TResult}.UpdateQueryData(TArg, TResult)"/>
    public bool UpdateQueryData(TArg arg, TResult resultData) => Cache.UpdateQueryData(arg, resultData);
}

public class QueryEndpoint<TResult> : QueryEndpoint<Unit, TResult>
{
    public QueryEndpoint(
        Func<Task<TResult>> queryFn,
        QueryEndpointOptions<TResult>? options = null
    ) : base(_ => queryFn(), options)
    {
    }

    public new Query<TResult> Use(QueryOptions<TResult> options)
    {
        return new Query<TResult>(Cache, options);
    }
}
