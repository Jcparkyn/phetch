﻿namespace Phetch.Tests;

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
}

public sealed record PollyRetryHandler(IAsyncPolicy Policy) : IRetryHandler
{
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct) =>
        Policy.ExecuteAsync(queryFn, ct);
}
