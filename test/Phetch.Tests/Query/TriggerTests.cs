namespace Phetch.Tests.Query;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Phetch.Core;
using Xunit;

public class TriggerTests
{
    [UIFact]
    public async Task Should_set_loading_states_correctly()
    {
        var tcs = new TaskCompletionSource<int>();
        var mut = new Endpoint<int, int>(
            (val, _) => tcs.Task
        ).Use();

        mut.Status.Should().Be(QueryStatus.Idle);
        mut.IsUninitialized.Should().BeTrue();
        mut.HasData.Should().BeFalse();

        // Fetch once
        var triggerTask1 = mut.TriggerAsync(10);

        mut.IsLoading.Should().BeTrue();
        mut.IsFetching.Should().BeTrue();

        await Task.Yield();
        tcs.SetResult(11);
        var result1 = await triggerTask1;

        result1.Should().Be(11);
        mut.Status.Should().Be(QueryStatus.Success);
        mut.IsSuccess.Should().BeTrue();
        mut.IsLoading.Should().BeFalse();
        mut.Data.Should().Be(11);
        mut.HasData.Should().BeTrue();

        tcs = new();
        // Fetch again
        var triggerTask2 = mut.TriggerAsync(20);

        mut.Status.Should().Be(QueryStatus.Loading);
        mut.IsLoading.Should().BeTrue();
        mut.IsFetching.Should().BeTrue();

        tcs.SetResult(21);
        var result2 = await triggerTask2;

        result2.Should().Be(21);
        mut.IsLoading.Should().BeFalse();
    }

    [UIFact]
    public async Task Should_handle_query_error()
    {
        var error = new IndexOutOfRangeException("message");
        var query = new MutationEndpoint<string>(
            (_, _) => Task.FromException(error)
        ).Use();

        await query.Invoking(x => x.TriggerAsync("test"))
            .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

        query.HasData.Should().BeFalse();
        query.Status.Should().Be(QueryStatus.Error);
        query.Error.Should().Be(error);

        query.IsError.Should().BeTrue();
        query.IsSuccess.Should().BeFalse();
        query.IsLoading.Should().BeFalse();
    }

    [UITheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_reset_state_after_cancel(bool awaitBeforeCancel)
    {
        var query = new MutationEndpoint<string>(
            (val, ct) => Task.Delay(1000, ct)
        ).Use();

        var task = query.Invoking(x => x.TriggerAsync("test"))
            .Should().ThrowExactlyAsync<TaskCanceledException>();
        if (awaitBeforeCancel)
        {
            await Task.Yield();
        }
        query.Cancel();

        await task;

        query.Status.Should().Be(QueryStatus.Idle);
        query.Error.Should().Be(null);
        query.HasData.Should().BeFalse();
        query.IsError.Should().BeFalse();
        query.IsSuccess.Should().BeFalse();
        query.IsLoading.Should().BeFalse();
    }

    [UIFact]
    public async Task Should_not_share_cache_between_triggered_queries()
    {
        var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(2);
        var endpoint = new Endpoint<int, string>(queryFn);

        var options = new QueryOptions<int, string>()
        {
            StaleTime = TimeSpan.FromMinutes(100),
        };
        var query1 = endpoint.Use(options);
        var query2 = endpoint.Use(options);
        sources[0].SetResult("10-1");
        sources[1].SetResult("10-2");
        var result1 = await query1.TriggerAsync(10);
        var result2 = await query2.TriggerAsync(10);

        result1.Should().Be("10-1");
        result2.Should().Be("10-2");

        query1.Data.Should().Be("10-1");
        query2.Data.Should().Be("10-2");

        queryFnCalls.Should().Equal(10, 10);
    }
}
