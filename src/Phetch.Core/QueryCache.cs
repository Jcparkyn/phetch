namespace Phetch.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal interface IQueryCache<TResult>
{
    void Remove(FixedQuery<TResult> query);
}

internal class QueryCache<TArg, TResult> : IQueryCache<TResult>
{
    internal Func<TArg, CancellationToken, Task<TResult>> QueryFn { get; }

    private readonly Dictionary<TArg, FixedQuery<TResult>> _cachedResponses = new();
    private readonly Dictionary<TArg, List<FixedQuery<TResult>>> _uncachedResponses = new();
    private readonly TimeSpan _cacheTime;

    public QueryCache(Func<TArg, CancellationToken, Task<TResult>> queryFn, TimeSpan cacheTime)
    {
        QueryFn = queryFn;
        _cacheTime = cacheTime;
    }

    public void InvalidateAll()
    {
        foreach (var (_, query) in _cachedResponses)
        {
            query?.Invalidate();
        }
    }

    public void Invalidate(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var query))
        {
            query?.Invalidate();
        }
    }

    internal void InvalidateWhere(Func<TArg, FixedQuery<TResult>, bool> predicate)
    {
        foreach (var (arg, query) in _cachedResponses)
        {
            if (predicate(arg, query))
            {
                query?.Invalidate();
            }
        }
    }

    public FixedQuery<TResult> GetOrAdd(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var value))
        {
            return value;
        }

        var newQuery = CreateQuery(arg, _cacheTime);

        _cachedResponses.Add(arg, newQuery);

        return newQuery;
    }

    public FixedQuery<TResult> AddUncached(TArg arg)
    {
        var newQuery = CreateQuery(arg, TimeSpan.Zero);

        if (_uncachedResponses.TryGetValue(arg, out var queries))
        {
            queries.Add(newQuery);
        }
        else
        {
            _uncachedResponses.Add(arg, new() { newQuery });
        }

        return newQuery;
    }

    private FixedQuery<TResult> CreateQuery(TArg arg, TimeSpan cacheTime)
    {
        return new FixedQuery<TResult>(this, (ct) => QueryFn(arg, ct), cacheTime);
    }

    /// <summary>
    /// Updates the response data for a given query, if it exists.
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="resultData"></param>
    /// <returns><c>true</c> if the query existed, otherwise <c>false</c>.</returns>
    public bool UpdateQueryData(TArg arg, TResult resultData)
    {
        var exists = false;
        if (_cachedResponses.TryGetValue(arg, out var result))
        {
            result.UpdateQueryData(resultData);
            exists = true;
        }
        if (_uncachedResponses.TryGetValue(arg, out var queries))
        {
            foreach (var query in queries)
            {
                query.UpdateQueryData(resultData);
            }
            exists = true;
        }
        return exists;
    }

    public void Remove(FixedQuery<TResult> query)
    {
        // TODO: Use key to remove
        var item = _cachedResponses.FirstOrDefault(kvp => kvp.Value == query);
        if (item.Value is not null)
            _cachedResponses.Remove(item.Key);
    }
}
