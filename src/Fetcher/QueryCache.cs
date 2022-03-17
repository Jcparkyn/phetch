namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IQueryCache<TResult>
{
    void Remove(FixedQuery<TResult> query);
}

public class QueryCache<TArg, TResult> : IQueryCache<TResult>
{
    private readonly Dictionary<TArg, FixedQuery<TResult>> _cachedResponses = new();
    private readonly Func<TArg, Task<TResult>> _queryFn;
    private readonly QueryMethodOptions<TResult> _options;

    public QueryCache(Func<TArg, Task<TResult>> queryFn, QueryMethodOptions<TResult>? options)
    {
        _queryFn = queryFn;
        _options = options ?? new();
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
        return new FixedQuery<TResult>(this, () => _queryFn(arg), _options);
    }

    public void UpdateQueryData(TArg arg, TResult resultData)
    {
        if (_cachedResponses.TryGetValue(arg, out var result))
        {
            result.UpdateQueryData(resultData);
        }
    }

    public void Remove(FixedQuery<TResult> query)
    {
        // TODO: Use key to remove
        var item = _cachedResponses.First(kvp => kvp.Value == query);
        _cachedResponses.Remove(item.Key);
    }
}
