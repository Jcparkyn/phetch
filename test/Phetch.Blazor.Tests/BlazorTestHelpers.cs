namespace Phetch.Blazor.Tests;

using System;
using System.Collections.Generic;

internal class BlazorTestHelpers
{
    public static (Action<T>, List<T>) MakeMonitoredAction<T>()
    {
        var actionCalls = new List<T>();
        var action = (T arg) =>
        {
            actionCalls.Add(arg);
        };
        return (action, actionCalls);
    }

    public static (Action, List<object?>) MakeMonitoredAction()
    {
        var actionCalls = new List<object?>();
        var action = () =>
        {
            actionCalls.Add(null);
        };
        return (action, actionCalls);
    }
}
