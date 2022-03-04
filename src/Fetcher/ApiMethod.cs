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

    //public void UpdateQueryData(TArg arg, TResult resultData)
    //{
    //    if (_cachedResponses.TryGetValue(arg, out var result))
    //    {
    //        _cachedResponses[arg] = result with
    //        {
    //            Data = resultData,
    //        };
    //        foreach (var stateChangedAction in result.OnStateChangedActions)
    //        {
    //            if (stateChangedAction.TryGetTarget(out var stateChanged))
    //            {
    //                stateChanged();
    //            }
    //            // TODO: Remove dead references
    //        }
    //        _cachedResponses.Remove(arg);
    //    }
    //}
}
