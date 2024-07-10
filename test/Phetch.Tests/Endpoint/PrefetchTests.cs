namespace Phetch.Tests.Endpoint;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Phetch.Core;
using Xunit;

public class PrefetchTests
{
    [UIFact]
    public async Task Should_prefetch_data()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);
        var prefetchResult = await endpoint.PrefetchAsync(1);

        queryFnCalls.Should().Equal(1);
        prefetchResult.Should().Be("1");

        var query = endpoint.Use(new()
        {
            StaleTime = TimeSpan.FromSeconds(10),
        });
        var setArgTask = query.SetArgAsyncInternal(1);

        // The query should have completed synchronously without re-fetching
        query.IsSuccess.Should().BeTrue();
        query.Data.Should().Be("1");
        queryFnCalls.Should().Equal(1);

        await setArgTask;
    }

    [UIFact]
    public async Task Should_do_nothing_if_query_exists()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);
        // Query in progress:
        var query1 = endpoint.Use();
        var setArgTask1 = query1.SetArgAsyncInternal(1);
        var prefetchResult1 = await endpoint.PrefetchAsync(1);
        prefetchResult1.Should().Be("1");
        await setArgTask1;

        query1.Data.Should().Be("1");
        queryFnCalls.Should().Equal(1);

        // Complete query:
        var query2 = endpoint.Use();
        await query2.SetArgAsyncInternal(1);
        var prefetchResult2 = await endpoint.PrefetchAsync(1);
        prefetchResult2.Should().Be("1");

        query2.Data.Should().Be("1");
        queryFnCalls.Should().Equal(1, 1);
    }

    [UIFact]
    public async Task Should_refetch_failed_query()
    {
        var queryFnCalls = new List<int>();
        var endpoint = new Endpoint<int, string>(
            async val =>
            {
                queryFnCalls.Add(val);
                await Task.Yield();
                throw new Exception("Test exception");
            }
        );

        var query = endpoint.Use(new()
        {
            StaleTime = TimeSpan.FromSeconds(10),
        });

        await query.Awaiting(q => q.SetArgAsyncInternal(1)).Should().ThrowAsync<Exception>();
        var prefetchTask = endpoint.Awaiting(e => e.PrefetchAsync(1)).Should().ThrowAsync<Exception>();

        queryFnCalls.Should().Equal(1, 1);
        query.IsFetching.Should().BeTrue();
        await prefetchTask;
        query.IsError.Should().BeTrue();
    }
}
