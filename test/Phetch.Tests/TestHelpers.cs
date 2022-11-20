namespace Phetch.Tests;

using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Phetch.Core;
using Polly;

public class TestHelpers
{
    /// <summary>
    /// Equivalent to <see cref="Task.FromResult{TResult}(TResult)"/>, but forces a yield so
    /// that the task doesn't complete synchronously.
    /// </summary>
    public static async Task<T> ReturnAsync<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    public static (Func<int, Task<string>> queryFn, IReadOnlyList<int> queryFnCalls) MakeTrackedQueryFn()
    {
        var queryFnCalls = new List<int>();
        var queryFn = async (int val) =>
        {
            queryFnCalls.Add(val);
            await Task.Yield();
            return val.ToString();
        };
        return (queryFn, queryFnCalls);
    }

    /// <summary>
    /// Makes a query function that can be called multiple times, using a different TaskCompletionSource each time.
    /// </summary>
    public static (Func<CancellationToken, Task<string>> queryFn, List<TaskCompletionSource<string>> sources) MakeCustomQueryFn(int numSources)
    {
        var sources = Enumerable.Range(0, numSources)
            .Select(_ => new TaskCompletionSource<string>())
            .ToList();

        var queryCount = 0;
        var queryFn = async (CancellationToken _) =>
        {
            if (queryCount > numSources)
                throw new Exception("Query function called too many times");
            var resultTask = sources[queryCount].Task;
            queryCount++;
            return await resultTask;
        };
        return (queryFn, sources);
    }

    /// <summary>
    /// Makes a query function that can be called multiple times, using a different TaskCompletionSource each time.
    /// </summary>
    public static (Func<int, Task<string>> queryFn, List<TaskCompletionSource<string>> sources, IReadOnlyList<int> queryFnCalls) MakeCustomTrackedQueryFn(int numSources)
    {
        var sources = Enumerable.Range(0, numSources)
            .Select(_ => new TaskCompletionSource<string>())
            .ToList();

        var queryFnCalls = new List<int>();
        var queryCount = 0;
        var queryFn = async (int val) =>
        {
            if (queryCount >= numSources)
                throw new Exception("Query function called too many times");
            var resultTask = sources[queryCount].Task;
            queryCount++;
            queryFnCalls.Add(val);
            return await resultTask;
        };
        return (queryFn, sources, queryFnCalls);
    }
}

public sealed record PollyRetryHandler(IAsyncPolicy Policy) : IRetryHandler
{
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct) =>
        Policy.ExecuteAsync(queryFn, ct);
}
