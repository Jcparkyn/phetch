namespace Phetch.Blazor.Tests;

using System;
using System.Collections.Generic;

internal static class BlazorTestHelpers
{
    public static (Action<T>, List<T>) MakeMonitoredAction<T>()
    {
        var actionCalls = new List<T>();
        void Action(T arg)
        {
            actionCalls.Add(arg);
        }
        return (Action, actionCalls);
    }

    public static (Action, List<object?>) MakeMonitoredAction()
    {
        var actionCalls = new List<object?>();
        void Action()
        {
            actionCalls.Add(null);
        }
        return (Action, actionCalls);
    }
}
