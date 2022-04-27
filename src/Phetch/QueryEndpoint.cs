namespace Phetch;

using System;
using System.Threading.Tasks;

public class QueryEndpoint<TArg, TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;

    public QueryEndpoint(
        Func<TArg, Task<TResult>> queryFn,
        QueryMethodOptions<TResult>? options = null)
    {
        options ??= new();
        _cache = new(queryFn, options);
    }

    public Query<TArg, TResult> Use(QueryObserverOptions<TResult> options)
    {
        return new Query<TArg, TResult>(_cache, options);
    }

    public Query<TArg, TResult> Use() => Use(new());

    public void InvalidateAll()
    {
        _cache.InvalidateAll();
    }

    public void Invalidate(TArg arg)
    {
        _cache.Invalidate(arg);
    }

    public void UpdateQueryData(TArg arg, TResult resultData) => _cache.UpdateQueryData(arg, resultData);
}
