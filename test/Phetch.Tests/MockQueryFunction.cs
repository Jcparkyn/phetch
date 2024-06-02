namespace Phetch.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Phetch.Core;

/// <summary>
/// Class for creating query functions for testing.
/// </summary>
/// <param name="numSources">
/// Number of TaskCompletionSources to create, which is usually equal to the number of times this
/// function will be called.
/// </param>
public class MockQueryFunction<TArg, TResult>()
{
    private Dictionary<int, TaskCompletionSource<TResult>> Sources { get; } = [];

    /// <summary>
    /// List of calls that have been made to this query function.
    /// </summary>
    public List<TArg> Calls { get; } = [];

    public async Task<TResult> Query(TArg arg)
    {
        var resultTask = GetSource(Calls.Count).Task;
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
        GetSource(call).SetResult(result);
    }

    public TaskCompletionSource<TResult> GetSource(int call)
    {
        if (Sources.TryGetValue(call, out var source))
        {
            return source;
        }
        var newSource = new TaskCompletionSource<TResult>();
        Sources[call] = newSource;
        return newSource;
    }
}

public class MockQueryFunction<TResult> : MockQueryFunction<Unit, TResult>
{
    public async Task<TResult> Query() => await Query(default);
}
