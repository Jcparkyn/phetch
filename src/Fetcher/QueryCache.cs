namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class QueryCache<TArg, TResult>
{
    private readonly Dictionary<TArg, FixedQuery<TResult>> _cachedResponses = new();
    private readonly Func<TArg, Task<TResult>> _queryFn;

    public QueryCache(Func<TArg, Task<TResult>> queryFn)
    {
        _queryFn = queryFn;
    }

    public void InvalidateAll()
    {
        foreach (var (_, query) in _cachedResponses)
        {
            query.Invalidate();
        }
    }

    public FixedQuery<TResult> GetOrAdd(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var value))
        {
            return value;
        }

        var newQuery = new FixedQuery<TResult>(
            () => _queryFn(arg),
            null);

        _cachedResponses.Add(arg, newQuery);

        return newQuery;
    }
}
