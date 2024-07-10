namespace Phetch.Tests.Endpoint;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Phetch.Core;
using Xunit;

public class DisposeTests
{
    [UIFact]
    public async Task Should_dispose_immediately_when_CacheTime_is_zero()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString()),
            options: new()
            {
                CacheTime = TimeSpan.Zero,
            }
        );
        var query = endpoint.Use();
        await query.SetArgAsyncInternal(1);
        endpoint.GetCachedQuery(1).Should().NotBeNull();

        await query.SetArgAsyncInternal(2);
        endpoint.GetCachedQuery(1).Should().BeNull();
        query.Dispose();
        endpoint.GetCachedQuery(2).Should().BeNull();
    }

    [UIFact]
    public async Task Should_dispose_after_CacheTime()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString()),
            options: new()
            {
                CacheTime = TimeSpan.FromSeconds(10),
                TimeProvider = timeProvider,
            }
        );
        var query = endpoint.Use();
        await query.SetArgAsyncInternal(1);
        query.Dispose();
        endpoint.GetCachedQuery(1).Should().NotBeNull();

        timeProvider.Advance(TimeSpan.FromSeconds(9));
        endpoint.GetCachedQuery(1).Should().NotBeNull();
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        endpoint.GetCachedQuery(1).Should().BeNull();
    }

    [UIFact]
    public async Task Should_cancel_dispose_if_observed_again()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString()),
            options: new()
            {
                CacheTime = TimeSpan.FromSeconds(10),
                TimeProvider = timeProvider,
            }
        );
        var query1 = endpoint.Use();
        await query1.SetArgAsyncInternal(1);
        query1.Dispose();
        endpoint.GetCachedQuery(1).Should().NotBeNull();

        timeProvider.Advance(TimeSpan.FromSeconds(9));
        var query2 = endpoint.Use();
        await query2.SetArgAsyncInternal(1);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        endpoint.GetCachedQuery(1).Should().NotBeNull();
    }
}
