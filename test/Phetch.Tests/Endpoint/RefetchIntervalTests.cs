namespace Phetch.Tests.Endpoint;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Phetch.Core;
using Xunit;

public class RefetchIntervalTests
{
    [UIFact]
    public async Task RefetchInterval_with_single_query()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query, options: new()
        {
            TimeProvider = timeProvider,
        });
        var query = endpoint.Use(options: new()
        {
            RefetchInterval = TimeSpan.FromSeconds(10),
        });
        qf.SetResult(0, "result0");

        await query.SetArgAsyncInternal(1);
        query.Status.Should().Be(QueryStatus.Success);
        query.Data.Should().Be("result0");

        // Before refetch interval passes
        timeProvider.Advance(TimeSpan.FromSeconds(9));
        query.IsFetching.Should().BeFalse();

        // After refetch interval passes
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        query.IsFetching.Should().BeTrue();

        qf.SetResult(1, "result1");

        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("result1");

        qf.Calls.Should().Equal(1, 1);

        // Next refetch interval
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        query.IsFetching.Should().BeTrue();
        qf.SetResult(2, "result2");
        query.IsFetching.Should().BeFalse();
    }

    [UIFact]
    public async Task RefetchInterval_with_two_queries()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query, options: new()
        {
            TimeProvider = timeProvider,
            DefaultStaleTime = TimeSpan.MaxValue,
        });
        List<TimeSpan> intervals = [
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(8),
            TimeSpan.MaxValue,
        ];
        var queries = intervals.Select(interval =>
            endpoint.Use(options: new()
            {
                RefetchInterval = interval,
            })).ToList();

        qf.SetResult(0, "result0");

        foreach (var query in queries)
            await query.SetArgAsyncInternal(1);

        qf.Calls.Should().Equal(1);
        foreach (var query in queries)
            query.Data.Should().Be("result0");

        // Should refetch after first interval
        timeProvider.Advance(TimeSpan.FromSeconds(9));
        qf.Calls.Should().Equal(1, 1);
        foreach (var query in queries)
            query.IsFetching.Should().BeTrue();
        qf.SetResult(1, "result1");

        // Shouldn't refetch after slower interval
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        foreach (var query in queries)
            query.IsFetching.Should().BeFalse();
    }

    [UIFact]
    public async Task Should_stop_fetching_when_query_is_removed()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query, options: new()
        {
            TimeProvider = timeProvider,
        });
        var query = endpoint.Use(options: new()
        {
            RefetchInterval = TimeSpan.FromSeconds(10),
        });

        qf.SetResult(0, "result0");
        await query.SetArgAsyncInternal(1);

        // After refetch interval passes
        timeProvider.Advance(TimeSpan.FromSeconds(11));
        query.IsFetching.Should().BeTrue();
        qf.SetResult(1, "result1");

        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("result1");
        qf.Calls.Should().Equal(1, 1);

        // Remove query
        query.Dispose();

        // Next refetch interval
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        query.IsFetching.Should().BeFalse();
    }

    [UIFact]
    public async Task Should_handle_interval_change_after_fetch()
    {
        var timeProvider = new PhetchFakeTimeProvider();
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query, options: new()
        {
            TimeProvider = timeProvider,
        });
        var query1 = endpoint.Use(options: new()
        {
            RefetchInterval = TimeSpan.FromSeconds(10),
        });

        qf.SetResult(0, "result0");
        await query1.SetArgAsyncInternal(1);

        // Before refetch interval passes
        timeProvider.Advance(TimeSpan.FromSeconds(9));

        // Change interval
        var query2 = endpoint.Use(options: new()
        {
            RefetchInterval = TimeSpan.FromSeconds(3),
        });
        qf.SetResult(1, "result1");
        await query2.SetArgAsyncInternal(1);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        query2.IsFetching.Should().BeFalse();

        // 3s interval should trigger, but not 10s interval.
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        query2.IsFetching.Should().BeTrue();
        qf.SetResult(2, "result2");

        qf.Calls.Should().Equal(1, 1, 1);
    }
}
