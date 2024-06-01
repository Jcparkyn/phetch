namespace Phetch.Core;

using System;
using System.Threading;

// Ideally, we'd use the built-in ITimeProvider from .NET 9, but we need to support netstandard2.0
// and the Microsoft.Bcl.TimeProvider package is too large. This is a minimal replacement for unit
// testing time handling.

internal interface ITimer : IDisposable
{
    void Change(TimeSpan dueTime, TimeSpan period);
}

internal interface ITimeProvider
{
    DateTimeOffset GetUtcNow();
    ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);
}

internal class DefaultTimer : ITimer
{
    private readonly Timer _timer;

    public DefaultTimer(Timer timer) => _timer = timer;

    public void Change(TimeSpan dueTime, TimeSpan period) => _timer.Change(dueTime, period);

    public void Dispose() => _timer.Dispose();
}

internal class DefaultTimeProvider : ITimeProvider
{
    public static DefaultTimeProvider Instance = new();

    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    public ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new DefaultTimer(new Timer(callback, state, dueTime, period));
    }
}
