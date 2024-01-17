namespace Phetch.Tests.Endpoint;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;

public class EndpointTests
{
    [UIFact]
    public async Task Should_create_valid_query()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString())
        );
        var query = endpoint.Use();
        var result = await query.SetArgAsync(10);

        using (new AssertionScope())
        {
            query.Options.Should().Be(QueryOptions<int, string>.Default);
            result.Should().Be("10");
            query.Data.Should().Be("10");
            endpoint.GetCachedQuery(10).Should().Be(query.CurrentQuery);
            endpoint.GetCachedQueryByKey(10).Should().Be(query.CurrentQuery);
        }
    }

    [UIFact]
    public async Task Should_share_cache_between_queries()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var onSuccessCalls = new List<string>();
        var endpoint = new Endpoint<int, string>(queryFn, new()
        {
            OnSuccess = e => onSuccessCalls.Add(e.Result),
        });

        var options = new QueryOptions() { StaleTime = TimeSpan.MaxValue };
        var query1 = endpoint.Use(options);
        var query2 = endpoint.Use(options);
        var query1Mon = query1.Monitor();
        var query2Mon = query2.Monitor();
        var result1 = await query1.SetArgAsync(10);
        var result2 = await query2.SetArgAsync(10);

        using (new AssertionScope())
        {
            result1.Should().Be("10");
            result2.Should().Be("10");

            query1.Data.Should().Be("10");
            query2.Data.Should().Be("10");

            queryFnCalls.Should().Equal(10);
            query1Mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("StateChanged"),
                e => e.EventName.Should().Be("Succeeded"),
                e => e.EventName.Should().Be("StateChanged")
            );
            query2Mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("Succeeded"),
                e => e.EventName.Should().Be("StateChanged")
            );
            onSuccessCalls.Should().Equal("10");
        }
    }

    [UIFact]
    public async Task Should_deduplicate_concurrent_queries()
    {
        var qf = new MockQueryFunction<int, string>(1);
        var onSuccessCalls = new List<string>();
        var endpoint = new Endpoint<int, string>(qf.Query, new()
        {
            OnSuccess = e => onSuccessCalls.Add(e.Result),
        });

        var query1 = endpoint.Use();
        var query2 = endpoint.Use();
        var query1Mon = query1.Monitor();
        var query2Mon = query2.Monitor();

        var task1 = query1.SetArgAsync(10);
        var task2 = query2.SetArgAsync(10);

        qf.SetResult(0, "10");

        await Task.WhenAll(task1, task2);

        query1Mon.GetRecordingFor("Succeeded").Should().HaveCount(1);
        query2Mon.GetRecordingFor("Succeeded").Should().HaveCount(1);
        qf.Calls.Should().Equal(10);
        onSuccessCalls.Should().Equal("10");
    }

    [UIFact]
    public async Task Invoke_should_work()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString())
        );
        var result = await endpoint.Invoke(2);
        result.Should().Be("2");
    }

    [UIFact]
    public async Task Should_handle_null_value_keys()
    {
        var endpoint = new Endpoint<int?, string>(
            val => TestHelpers.ReturnAsync(val?.ToString() ?? "null")
        );
        var query1 = endpoint.Use();
        var result1 = await query1.SetArgAsync(10);
        var queryNull = endpoint.Use();
        var resultNull = await queryNull.SetArgAsync(null);

        using (new AssertionScope())
        {
            result1.Should().Be("10");
            query1.Data.Should().Be("10");
            resultNull.Should().Be("null");
            queryNull.Data.Should().Be("null");

            endpoint.GetCachedQuery(10).Should().Be(query1.CurrentQuery);
            endpoint.GetCachedQuery(null).Should().Be(queryNull.CurrentQuery);
            endpoint.TryGetCachedResult(10, out var data).Should().BeTrue();
            endpoint.TryGetCachedResult(11, out _).Should().BeFalse();
            data.Should().Be("10");
        }
    }

    [UIFact]
    public async Task Should_handle_null_reference_keys()
    {
        var endpoint = new Endpoint<string?, string>(
            val => TestHelpers.ReturnAsync(val?.ToString() ?? "null")
        );
        var query1 = endpoint.Use();
        var result1 = await query1.SetArgAsync("10");
        var queryNull = endpoint.Use();
        var resultNull = await queryNull.SetArgAsync(null);

        using (new AssertionScope())
        {
            result1.Should().Be("10");
            query1.Data.Should().Be("10");
            resultNull.Should().Be("null");
            queryNull.Data.Should().Be("null");

            endpoint.GetCachedQuery("10").Should().Be(query1.CurrentQuery);
            endpoint.GetCachedQuery(null).Should().Be(queryNull.CurrentQuery);
            endpoint.TryGetCachedResult("10", out var data).Should().BeTrue();
            endpoint.TryGetCachedResult("11", out _).Should().BeFalse();
            data.Should().Be("10");
        }
    }

    [UIFact]
    public async Task GetAllQueries_should_work()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString())
        );
        var query1 = endpoint.Use();
        var query2 = endpoint.Use();
        var trigger1 = endpoint.Use();
        var trigger2 = endpoint.Use();

        await query1.SetArgAsync(1);
        await query2.SetArgAsync(2);
        await trigger1.TriggerAsync(1);
        await trigger2.TriggerAsync(2);

        endpoint.Cache.GetAllQueries(2).Should().Equal(
            query2.CurrentQuery!,
            trigger2.CurrentQuery!);
    }

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
        await query.SetArgAsync(1);
        endpoint.GetCachedQuery(1).Should().NotBeNull();

        await query.SetArgAsync(2);
        endpoint.GetCachedQuery(1).Should().BeNull();
        query.Dispose();
        endpoint.GetCachedQuery(2).Should().BeNull();
    }
}
