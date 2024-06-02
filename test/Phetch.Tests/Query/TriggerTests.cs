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
        var qf = new MockQueryFunction<int, string>();
        var query = new Endpoint<int, string>(qf.Query).Use();
        var mon = query.Monitor();

        query.Status.Should().Be(QueryStatus.Idle);
        query.IsUninitialized.Should().BeTrue();
        query.HasData.Should().BeFalse();

        // Fetch once
        var triggerTask1 = query.TriggerAsync(10);

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged")
        );
        mon.Clear();

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        await Task.Yield();
        qf.GetSource(0).SetResult("10");
        var result1 = await triggerTask1;

        result1.Should().Be("10");
        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.Data.Should().Be("10");
        query.HasData.Should().BeTrue();

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal("10");
            });
        mon.Clear();

        // Fetch again
        var triggerTask2 = query.TriggerAsync(20);

        query.Status.Should().Be(QueryStatus.Loading);
        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal([null]);
            });
        mon.Clear();

        qf.GetSource(1).SetResult("20");
        var result2 = await triggerTask2;

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal("20");
            });

        result2.Should().Be("20");
        query.IsLoading.Should().BeFalse();
    }

    [UIFact]
    public async Task Should_handle_query_error()
    {
        var error = new IndexOutOfRangeException("message");
        var query = new ResultlessEndpoint<string>(
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
        var query = new ResultlessEndpoint<string>(
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
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query);

        var options = new QueryOptions<int, string>()
        {
            StaleTime = TimeSpan.FromMinutes(100),
        };
        var query1 = endpoint.Use(options);
        var query2 = endpoint.Use(options);
        qf.GetSource(0).SetResult("10-1");
        qf.GetSource(1).SetResult("10-2");
        var result1 = await query1.TriggerAsync(10);
        var result2 = await query2.TriggerAsync(10);

        result1.Should().Be("10-1");
        result2.Should().Be("10-2");

        query1.Data.Should().Be("10-1");
        query2.Data.Should().Be("10-2");

        qf.Calls.Should().Equal(10, 10);
    }

    [UIFact]
    public async Task Should_inkove_success_callback()
    {
        var endpoint = new Endpoint<int, string>(
            x => TestHelpers.ReturnAsync(x.ToString())
        );

        var query = endpoint.Use();
        QuerySuccessEventArgs<int, string>? args = null;
        _ = await query.TriggerAsync(10, onSuccess: e => args = e);

        args.Should().NotBeNull();
        args!.Result.Should().Be("10");
        args.Arg.Should().Be(10);
    }

    [UIFact]
    public async Task Should_inkove_failure_callback()
    {
        var endpoint = new Endpoint<int, string>(
            x => throw new Exception("boom")
        );

        var query = endpoint.Use();
        QueryFailureEventArgs<int>? args = null;
        await query.Invoking(q => q.TriggerAsync(10, onFailure: e => args = e)).Should()
            .ThrowAsync<Exception>().WithMessage("boom");

        args.Should().NotBeNull();
        args!.Exception.Message.Should().Be("boom");
        args.Arg.Should().Be(10);
    }

    [UIFact]
    public async Task Should_validate_params()
    {
        var query = null as Query<int, string>;
        var act = () => query!.TriggerAsync(0, onSuccess: _ => { });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [UIFact]
    public async Task Should_not_inkove_failure_callback_when_cancelled()
    {
        var endpoint = new Endpoint<int, string>(
            async (x, ct) =>
            {
                await Task.Delay(1000, ct);
                return x.ToString();
            }
        );

        var query = endpoint.Use();
        int calls = 0;

        var task = query.Invoking(q => q.TriggerAsync(10, onFailure: _ => calls++)).Should()
            .ThrowAsync<OperationCanceledException>();
        query.Cancel();
        await task;

        calls.Should().Be(0);
    }
}
