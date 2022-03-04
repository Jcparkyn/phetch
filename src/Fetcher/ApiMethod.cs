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

    public QueryHandle<TArg, TResult> Use()
    {
        return new QueryHandle<TArg, TResult>(_cache);
    }

    public void InvalidateAll()
    {
        _cache.InvalidateAll();
    }

    public void Invalidate(TArg arg)
    {
        throw new NotImplementedException();
    }

    public void UpdateQueryData(TArg arg, TResult resultData) => _cache.UpdateQueryData(arg, resultData);
}
