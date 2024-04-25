namespace Phetch.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Phetch.Core;

/// <summary>
/// Class for creating query functions for testing.
/// </summary>
/// <param name="numSources">
/// Number of TaskCompletionSources to create, which is usually equal to the number of times this
/// function will be called.
/// </param>
public class MockQueryFunction<TArg, TResult>(int numSources)
{
    public List<TaskCompletionSource<TResult>> Sources { get; } = Enumerable.Range(0, numSources)
        .Select(_ => new TaskCompletionSource<TResult>())
        .ToList();

    /// <summary>
    /// List of calls that have been made to this query function.
    /// </summary>
    public List<TArg> Calls { get; } = [];

    private int _queryCount = 0;

    public async Task<TResult> Query(TArg arg)
    {
        if (_queryCount >= Sources.Count)
            throw new Exception("Query function called too many times");
        var resultTask = Sources[_queryCount].Task;
        _queryCount++;
        Calls.Add(arg);

        if (!Debugger.IsAttached)
        {
            // Stop early if we're not debugging and it takes a while, we've probably forgotten to set a result before awaiting.
            return await resultTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        return await resultTask;
    }

    public void SetResult(int call, TResult result)
    {
        Sources[call].SetResult(result);
    }

    public void SetResult(int call, Func<TArg, TResult> queryFn)
    {
        Sources[call].SetResult(queryFn(Calls[call]));
    }
}

public class MockQueryFunction<TResult>(int numSources) : MockQueryFunction<Unit, TResult>(numSources)
{
    public async Task<TResult> Query() => await Query(default);
}
