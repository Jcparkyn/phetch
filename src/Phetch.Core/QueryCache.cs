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
        foreach (var query in _cachedResponses.Values)
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

    internal void InvalidateWhere(Func<FixedQuery<TArg, TResult>, bool> predicate)
    {
        foreach (var query in _cachedResponses.Values)
        {
            if (predicate(query))
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

    public void UpdateQueryData(TArg arg, TResult resultData, bool addIfNotExists)
    {
        UpdateQueryData(arg, _ => resultData, addIfNotExists);
    }

    public void UpdateQueryData(TArg arg, Func<FixedQuery<TArg, TResult>, TResult> dataSelector, bool addIfNotExists)
    {
        if (_cachedResponses.TryGetValue(arg, out var query1))
        {
            query1.UpdateQueryData(dataSelector(query1));
        }
        else if (addIfNotExists)
        {
            var newQuery = CreateQuery(arg, _endpointOptions);
            newQuery.UpdateQueryData(dataSelector(newQuery));
            _cachedResponses.Add(arg, newQuery);
        }
        if (_uncachedResponses.TryGetValue(arg, out var queries))
        {
            foreach (var query2 in queries)
            {
                query2.UpdateQueryData(dataSelector(query2));
            }
        }
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

    public FixedQuery<TArg, TResult>? GetCachedQuery(TArg arg)
    {
        return _cachedResponses.TryGetValue(arg, out var query) ? query : null;
    }

    public IEnumerable<FixedQuery<TArg, TResult>> GetAllQueries(TArg arg)
    {
        if (_cachedResponses.TryGetValue(arg, out var query1))
        {
            yield return query1;
        }
        if (_uncachedResponses.TryGetValue(arg, out var queries))
        {
            foreach (var query2 in queries)
            {
                yield return query2;
            }
        }
    }
}
