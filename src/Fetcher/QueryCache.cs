namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class QueryCache<TArg, TResult>
{
    private readonly Dictionary<TArg, FixedQuery<TResult>> _cachedResponses = new();
    private readonly Func<TArg, Task<TResult>> _queryFn;
    private readonly QueryOptions<TResult> _options;

    public QueryCache(Func<TArg, Task<TResult>> queryFn, QueryOptions<TResult> options)
    {
        _queryFn = queryFn;
        _options = options;
    }

    public void InvalidateAll()
    {
        foreach (var (_, query) in _cachedResponses)
        {
            query.Invalidate();
        }
    }

    public void Invalidate(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var query))
        {
            query?.Invalidate();
        }
    }

    public FixedQuery<TResult> GetOrAdd(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var value))
        {
            return value;
        }

        var newQuery = CreateQuery(arg);

        _cachedResponses.Add(arg, newQuery);

        return newQuery;
    }

    private FixedQuery<TResult> CreateQuery(TArg arg)
    {
        return new FixedQuery<TResult>(() => _queryFn(arg), _options);
    }

    public void UpdateQueryData(TArg arg, TResult resultData)
    {
        if (_cachedResponses.TryGetValue(arg, out var result))
        {
            result.UpdateQueryData(resultData);
        }
    }
}
