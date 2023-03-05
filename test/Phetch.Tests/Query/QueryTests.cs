namespace Phetch.Tests.Query;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;
using static TestHelpers;

public class QueryTests
{
    [UIFact]
    public async Task Should_work_with_basic_query()
    {
        var query = new ParameterlessEndpoint<string>(
            _ => ReturnAsync("test")
        ).Use();
        var result = await query.SetArgAsync(default);
        result.Should().Be("test");
        query.Data.Should().Be("test");
        AssertIsSuccessState(query);
    }

    [UIFact]
    public async Task SetArg_should_set_loading_states_correctly()
    {
        var tcs = new TaskCompletionSource<string>();
        var query = new ParameterlessEndpoint<string>(
            _ => tcs.Task
        ).Use();
        using var mon = query.Monitor();

        query.Status.Should().Be(QueryStatus.Idle);
        query.IsUninitialized.Should().BeTrue();
        query.HasData.Should().BeFalse();

        // Fetch once
        var refetchTask = query.SetArgAsync(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        tcs.SetResult("test");
        var result = await refetchTask;
        result.Should().Be("test");

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.HasData.Should().BeTrue();

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged")
        );
        mon.Clear();

        tcs = new();
        // Fetch again
        var refetchTask2 = query.RefetchAsync();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeTrue();

        tcs.SetResult("test");
        await refetchTask2;

        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged")
        );
        mon.Clear();
    }

    [UITheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetArg_should_reset_state_after_cancel_with_cancelable_query(bool awaitBeforeCancel)
    {
        var tcs = new TaskCompletionSource<string>();
        var query = new Endpoint<int, string>(
            (val, ct) => tcs.Task.WaitAsync(ct)
        ).Use();

        using var mon = query.Monitor();

        var task = query.Invoking(x => x.SetArgAsync(1))
            .Should().ThrowExactlyAsync<TaskCanceledException>();
        if (awaitBeforeCancel)
        {
            await Task.Yield();
        }
        query.Cancel();

        using (new AssertionScope())
        {
            AssertIsIdleState(query);
            mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("StateChanged")
            );
        }
        await task;
        AssertIsIdleState(query);
    }

    [UITheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetArg_should_reset_state_after_cancel_with_uncancelable_query(bool awaitBeforeCancel)
    {
        var tcs = new TaskCompletionSource<string>();
        var query = new Endpoint<int, string>(
            (val, _) => tcs.Task
        ).Use();

        using var mon = query.Monitor();

        var task = query.SetArgAsync(1);
        if (awaitBeforeCancel)
        {
            await Task.Yield();
        }
        query.Cancel();

        using (new AssertionScope())
        {
            AssertIsIdleState(query);
            mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("StateChanged")
            );
        }
        tcs.SetResult("1");
        (await task).Should().Be("1");
        AssertIsIdleState(query);
    }

    [UIFact]
    public async Task Should_handle_query_error()
    {
        var error = new IndexOutOfRangeException("BOOM!");
        var query = new ParameterlessEndpoint<string>(
            _ => Task.FromException<string>(error)
        ).Use();
        using var mon = query.Monitor();

        await query.Invoking(x => x.SetArgAsync(default))
            .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

        using (new AssertionScope())
        {
            query.Data.Should().BeNull();
            query.Status.Should().Be(QueryStatus.Error);
            query.Error.Should().Be(error);
            query.IsError.Should().BeTrue();
            query.IsSuccess.Should().BeFalse();
            query.IsLoading.Should().BeFalse();
            mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("Failed"),
                e => e.EventName.Should().Be("StateChanged")
            );
        }
    }

    [UIFact]
    public async Task Should_handle_exception_in_success_callbacks()
    {
        var query = new Endpoint<int, string>(
            val => ReturnAsync(val.ToString()),
            options: new()
            {
                OnSuccess = _ => throw new Exception("endpoint"),
            }
        ).Use(
            new()
            {
                OnSuccess = _ => throw new Exception("query"),
            }
        );

        var result = await query.SetArgAsync(1);
        result.Should().Be("1");
        AssertIsSuccessState(query);
        // TODO: We can't easily assert event calls here, because FluentAssertions doesn't handle exceptions properly yet.
        // See https://github.com/fluentassertions/fluentassertions/pull/1954
    }

    [UIFact]
    public async Task Should_handle_exception_in_failure_callbacks()
    {
        var query = new Endpoint<int, string>(
            val => Task.FromException<string>(new IndexOutOfRangeException("BOOM!")),
            options: new()
            {
                OnFailure = _ => throw new Exception("endpoint"),
            }
        ).Use(
            new()
            {
                OnFailure = _ => throw new Exception("query"),
            }
        );

        await query.Invoking(x => x.SetArgAsync(1))
           .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

        query.Status.Should().Be(QueryStatus.Error);
        query.IsError.Should().BeTrue();
        query.IsSuccess.Should().BeFalse();
        query.IsLoading.Should().BeFalse();
        // TODO: We can't easily assert event calls here, because FluentAssertions doesn't handle exceptions properly yet.
        // See https://github.com/fluentassertions/fluentassertions/pull/1954
    }

    [UIFact]
    public async Task Should_always_keep_most_recent_data()
    {
        // Timing:
        // t0 -------- [keep]
        // t1     --------- [keep]
        //        ^ refetch

        var (queryFn, sources) = MakeCustomQueryFn(2);
        var query = new ParameterlessEndpoint<string>(
            queryFn
        ).Use();

        query.Status.Should().Be(QueryStatus.Idle);

        query.SetArg(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        query.Refetch();

        sources[0].SetResult("test0");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeTrue();
        query.Data.Should().Be("test0");

        sources[1].SetResult("test1");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("test1");
    }

    // This test exists because the previous implementation used to intentionally ignore the second result.
    // This was later decided to be unnecessary, so now we keep both results (when the arguments are the same).
    [UIFact]
    public async Task Should_keep_newer_return_from_same_arg()
    {
        // Timing:
        // t0 ------------------- [keep]
        // t1     --------- [keep]
        //        ^ refetch

        var (queryFn, sources) = MakeCustomQueryFn(2);
        var query = new ParameterlessEndpoint<string>(
            queryFn
        ).Use();

        query.Status.Should().Be(QueryStatus.Idle);

        query.SetArg(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        query.Refetch();

        sources[1].SetResult("test1");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("test1");

        sources[0].SetResult("test0");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("test0");
    }

    private static void AssertIsIdleState<TArg, TResult>(Query<TArg, TResult> query)
    {
        query.Status.Should().Be(QueryStatus.Idle);
        query.Error.Should().Be(null);
        query.HasData.Should().BeFalse();
        query.IsError.Should().BeFalse();
        query.IsSuccess.Should().BeFalse();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
    }

    private static void AssertIsSuccessState<TArg, TResult>(Query<TArg, TResult> query)
    {
        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.IsError.Should().BeFalse();
        query.Error.Should().BeNull();
    }
}
