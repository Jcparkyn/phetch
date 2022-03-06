namespace Fetcher;

using System;
using System.Threading.Tasks;

public class ApiMethod<TArg, TResult>
{
    private readonly QueryCache<TArg, TResult> _cache;

    public ApiMethod(
        Func<TArg, Task<TResult>> queryFn)
    {
        _cache = new(queryFn);
    }

    public QueryObserver<TArg, TResult> Use()
    {
        return new QueryObserver<TArg, TResult>(_cache);
    }

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
