namespace Phetch.Core;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class QueryCache<TArg, TResult>
{
    internal Func<TArg, CancellationToken, Task<TResult>> QueryFn { get; }

    private readonly Dictionary<TArg, FixedQuery<TArg, TResult>> _cachedResponses = new();
    private readonly Dictionary<TArg, List<FixedQuery<TArg, TResult>>> _uncachedResponses = new();
    private readonly EndpointOptions<TArg, TResult> _endpointOptions;

    public QueryCache(Func<TArg, CancellationToken, Task<TResult>> queryFn, EndpointOptions<TArg, TResult> endpointOptions)
    {
        QueryFn = queryFn;
        _endpointOptions = endpointOptions;
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

    internal void InvalidateWhere(Func<TArg, FixedQuery<TArg, TResult>, bool> predicate)
    {
        foreach (var (arg, query) in _cachedResponses)
        {
            if (predicate(arg, query))
            {
                query?.Invalidate();
            }
        }
    }

    public FixedQuery<TArg, TResult> GetOrAdd(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var value))
        {
            return value;
        }

        var newQuery = CreateQuery(arg, _endpointOptions);

        _cachedResponses.Add(arg, newQuery);

        return newQuery;
    }

    public FixedQuery<TArg, TResult> AddUncached(TArg arg)
    {
        var newQuery = CreateQuery(arg, _endpointOptions with { CacheTime = TimeSpan.Zero });

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

    private FixedQuery<TArg, TResult> CreateQuery(TArg arg, EndpointOptions<TArg, TResult> endpointOptions)
    {
        return new FixedQuery<TArg, TResult>(this, QueryFn, arg, endpointOptions);
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

    public void Remove(FixedQuery<TArg, TResult> query)
    {
        if (_cachedResponses.TryGetValue(query.Arg, out var cachedQuery) && cachedQuery == query)
        {
            _cachedResponses.Remove(query.Arg);
        }
        if (_uncachedResponses.TryGetValue(query.Arg, out var uncachedQueries))
        {
            uncachedQueries.RemoveAll(x => x == query);
        }
    }
}
