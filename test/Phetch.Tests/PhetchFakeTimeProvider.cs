namespace Phetch.Tests;

using System;
using System.Threading;
using Microsoft.Extensions.Time.Testing;
using Phetch.Core;
using IPhetchTimer = Core.ITimer;
using ISystemTimer = System.Threading.ITimer;

class PhetchFakeTimer(ISystemTimer timer) : IPhetchTimer
{
    public void Change(TimeSpan dueTime, TimeSpan period) => timer.Change(dueTime, period);

    public void Dispose() => timer.Dispose();
}

class PhetchFakeTimeProvider : FakeTimeProvider, ITimeProvider
{
    IPhetchTimer ITimeProvider.CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new PhetchFakeTimer(CreateTimer(callback, state, dueTime, period));
    }
}
