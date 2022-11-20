namespace Phetch.Tests.Endpoint;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;

public class InvalidateTests
{
    [UITheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalidate_should_rerun_query(bool invalidateWhileRunning)
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);

        var query1 = endpoint.Use();
        var query2 = endpoint.Use();

        await query1.SetArgAsync(1);
        var setArg2Task = query2.SetArgAsync(2);
        if (!invalidateWhileRunning)
        {
            await setArg2Task;
        }

        queryFnCalls.Should().Equal(1, 2);

        endpoint.Invalidate(2);

        query1.IsFetching.Should().BeFalse();
        query2.IsFetching.Should().BeTrue();

        await setArg2Task;

        queryFnCalls.Should().Equal(1, 2, 2);

        endpoint.InvalidateAll();

        queryFnCalls.Should().Equal(1, 2, 2, 1, 2);
    }

    [UIFact]
    public async Task InvalidateWhere_should_rerun_query()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);

        var query1 = endpoint.Use();
        var query2 = endpoint.Use();

        await query1.SetArgAsync(1);
        await query2.SetArgAsync(2);

        endpoint.InvalidateWhere(q => q.Arg == 1);

        using (new AssertionScope())
        {
            query1.IsFetching.Should().BeTrue();
            query2.IsFetching.Should().BeFalse();

            queryFnCalls.Should().Equal(1, 2, 1);
        }

        endpoint.InvalidateAll();

        queryFnCalls.Should().Equal(1, 2, 1, 1, 2);
    }

    [UIFact]
    public async Task Invalidate_should_do_nothing_if_arg_unused()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);

        var query1 = endpoint.Use();
        var query2 = endpoint.Use();

        await query1.SetArgAsync(1);
        await query2.SetArgAsync(2);

        endpoint.Invalidate(3);

        using (new AssertionScope())
        {
            query1.IsFetching.Should().BeFalse();
            query2.IsFetching.Should().BeFalse();

            queryFnCalls.Should().Equal(1, 2);
        }
    }

    [UIFact]
    public async Task Invalidate_should_mark_invalidated_if_query_not_observed()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new Endpoint<int, string>(queryFn);

        var query1 = endpoint.Use(new()
        {
            StaleTime = TimeSpan.MaxValue,
        });

        await query1.SetArgAsync(1);

        query1.Detach();

        endpoint.Invalidate(1);
        queryFnCalls.Should().Equal(1);

        var query2 = endpoint.Use();
        var setArgTask = query2.SetArgAsync(1);

        using (new AssertionScope())
        {
            setArgTask.IsCompleted.Should().BeFalse();
            query2.IsFetching.Should().BeTrue();
            query2.Data.Should().Be("1");

            await setArgTask;
            query2.IsFetching.Should().BeFalse();
            query2.Data.Should().Be("1");
            queryFnCalls.Should().Equal(1, 1);
        }
    }
}
