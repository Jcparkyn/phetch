﻿namespace Phetch.Tests.Query;

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
        var result = await query.SetArgAsyncInternal(default);
        result.Should().Be("test");
        query.Data.Should().Be("test");
        AssertIsSuccessState(query);
    }

    [UIFact]
    public async Task SetArg_should_set_loading_states_correctly_for_same_arg()
    {
        var tcs = new TaskCompletionSource<string>();
        var query = new ParameterlessEndpoint<string>(
            _ => tcs.Task
        ).Use();
        using var mon = query.Monitor();

        query.Status.Should().Be(QueryStatus.Idle);
        query.IsUninitialized.Should().BeTrue();
        query.HasData.Should().BeFalse();
        query.Data.Should().BeNull();
        query.LastData.Should().BeNull();

        // Fetch once
        var fetchTask = query.SetArgAsyncInternal(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        tcs.SetResult("test");
        var result = await fetchTask;
        result.Should().Be("test");

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.HasData.Should().BeTrue();
        query.Data.Should().Be("test");
        query.LastData.Should().Be("test");

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged"),
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal("test");
            }
        );
        mon.Clear();

        tcs = new();
        // Fetch again
        var refetchTask = query.RefetchAsync();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeTrue();

        tcs.SetResult("test");
        await refetchTask;

        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged"),
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged")
        );
    }

    [UIFact]
    public async Task SetArg_should_set_loading_states_correctly_for_different_arg()
    {
        var tcs = new TaskCompletionSource<string>();
        var query = new Endpoint<int, string>(
            val => tcs.Task
        ).Use();
        using var mon = query.Monitor();

        query.Arg.Should().Be(0);

        // Fetch once
        var fetchTask = query.SetArgAsyncInternal(1);
        query.Arg.Should().Be(1);
        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        tcs.SetResult("one");
        var result = await fetchTask;
        result.Should().Be("one");

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.HasData.Should().BeTrue();
        query.Data.Should().Be("one");
        query.LastData.Should().Be("one");

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged"),
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged"),
            e => e.EventName.Should().Be("DataChanged")
        );
        mon.Clear();

        tcs = new();
        // Fetch again
        var refetchTask = query.SetArgAsyncInternal(2);

        query.Arg.Should().Be(2);
        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();
        query.Data.Should().BeNull();
        query.LastData.Should().Be("one");

        tcs.SetResult("two");
        await refetchTask;

        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("two");
        query.LastData.Should().Be("two");

        mon.OccurredEvents.Should().SatisfyRespectively(
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal([null]); // Normal params syntax breaks something here, so use an explicit array.
            },
            e => e.EventName.Should().Be("Succeeded"),
            e => e.EventName.Should().Be("StateChanged"),
            e =>
            {
                e.EventName.Should().Be("DataChanged");
                e.Parameters.Should().Equal("two");
            }
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

        var task = query.Invoking(x => x.SetArgAsyncInternal(1))
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
                e => e.EventName.Should().Be("StateChanged"),
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

        var task = query.SetArgAsyncInternal(1);
        if (awaitBeforeCancel)
        {
            await Task.Yield();
        }
        query.Cancel();

        using (new AssertionScope())
        {
            AssertIsIdleState(query);
            mon.OccurredEvents.Should().SatisfyRespectively(
                e => e.EventName.Should().Be("StateChanged"),
                e => e.EventName.Should().Be("StateChanged")
            );
        }
        tcs.SetResult("1");
        (await task).Should().Be("1");
        AssertIsIdleState(query);
    }

    [UIFact]
    public void SetArg_should_not_throw_when_query_fails_synchronously()
    {
        var query = new Endpoint<int, string>(
            val => throw new IndexOutOfRangeException("BOOM!")
        ).Use();
        query.SetArg(1); // Should not throw
        query.Error!.Message.Should().Be("BOOM!");
    }

    [UIFact]
    public async Task Should_handle_query_error()
    {
        var error = new IndexOutOfRangeException("BOOM!");
        var query = new ParameterlessEndpoint<string>(
            _ => Task.FromException<string>(error)
        ).Use();
        using var mon = query.Monitor();

        await query.Invoking(x => x.SetArgAsyncInternal(default))
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

        var result = await query.SetArgAsyncInternal(1);
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

        await query.Invoking(x => x.SetArgAsyncInternal(1))
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

        var qf = new MockQueryFunction<string>();
        var query = new ParameterlessEndpoint<string>(
            qf.Query
        ).Use();

        query.Status.Should().Be(QueryStatus.Idle);

        query.SetArg(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        query.Refetch();

        qf.GetSource(0).SetResult("test0");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeTrue();
        query.Data.Should().Be("test0");

        qf.GetSource(1).SetResult("test1");
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

        var qf = new MockQueryFunction<string>();
        var query = new ParameterlessEndpoint<string>(
            qf.Query
        ).Use();

        query.Status.Should().Be(QueryStatus.Idle);

        query.SetArg(default);

        query.IsLoading.Should().BeTrue();
        query.IsFetching.Should().BeTrue();

        query.Refetch();

        qf.GetSource(1).SetResult("test1");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsSuccess.Should().BeTrue();
        query.IsLoading.Should().BeFalse();
        // Maybe this should be true? But not very important and hard to implement.
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("test1");

        qf.GetSource(0).SetResult("test0");
        await Task.Yield();

        query.Status.Should().Be(QueryStatus.Success);
        query.IsLoading.Should().BeFalse();
        query.IsFetching.Should().BeFalse();
        query.Data.Should().Be("test0");
    }

    [UIFact]
    public async Task Refetch_should_throw_if_uninitialized()
    {
        var query = new ParameterlessEndpoint<string>(
            _ => ReturnAsync("test")
        ).Use();

        query.Invoking(q => q.Refetch())
            .Should()
            .ThrowExactly<InvalidOperationException>();

        await query.Invoking(q => q.RefetchAsync())
            .Should()
            .ThrowExactlyAsync<InvalidOperationException>();
    }

    [UIFact]
    public async Task Invoke_should_work()
    {
        var query = new Endpoint<int, string>(
            val => ReturnAsync(val.ToString())
        ).Use();
        var result = await query.Invoke(2);
        result.Should().Be("2");
    }

    [UIFact]
    public async Task SetArg_should_always_refetch_if_error()
    {
        var qf = new MockQueryFunction<int, string>();
        var endpoint = new Endpoint<int, string>(qf.Query, new()
        {
            // Disable automatic refetching
            DefaultStaleTime = TimeSpan.MaxValue,
        });
        var query = endpoint.Use();

        // Trigger an initial success. This ensures that _dataUpdatedAt is set, because otherwise
        // this test passes "for free".
        var task1 = query.SetArgAsyncInternal(0);
        qf.GetSource(0).SetResult("0");
        await task1;

        // Refetch with failure
        var task2 = query.RefetchAsync();
        qf.GetSource(1).SetException(new IndexOutOfRangeException("BOOM!"));
        (await task2).Error.Should().BeOfType<IndexOutOfRangeException>();

        // Currently, setting the same arg twice will never refetch, which is intentional.
        // Instead we just change the arg twice.
        _ = query.SetArgAsyncInternal(1);
        var task3 = query.SetArgAsyncInternal(0);
        qf.GetSource(3).SetResult("0 again");
        await task3;

        using (new AssertionScope())
        {
            query.Data.Should().Be("0 again");
            AssertIsSuccessState(query);
            qf.Calls.Should().Equal(0, 0, 1, 0);
        }
    }

    [UIFact]
    public async Task LastData_success_then_arg_change_error()
    {
        var qf = new MockQueryFunction<int, string>();
        var query = new Endpoint<int, string>(qf.Query).Use();

        query.LastData.Should().BeNull();

        // Fetch once
        var task1 = query.SetArgAsyncInternal(0);
        query.LastData.Should().BeNull();

        qf.GetSource(0).SetResult("0"); await task1;
        query.LastData.Should().Be("0");

        // Change arg
        var task2 = query.SetArgAsyncInternal(1);
        query.LastData.Should().Be("0");

        qf.GetSource(1).SetException(new Exception("boom"));
        await task2.Invoking(t => t).Should().ThrowExactlyAsync<Exception>();
        query.LastData.Should().Be("0");
    }

    [UIFact]
    public async Task LastData_refetch_error_then_arg_change()
    {
        var qf = new MockQueryFunction<int, string>();
        var query = new Endpoint<int, string>(qf.Query).Use();

        query.LastData.Should().BeNull();

        // Fetch once
        var task1 = query.SetArgAsyncInternal(0);
        query.LastData.Should().BeNull();

        qf.GetSource(0).SetResult("0"); await task1;
        query.LastData.Should().Be("0");

        // Refetch with failure
        var task2 = query.RefetchAsync();
        query.LastData.Should().Be("0");

        qf.GetSource(1).SetException(new Exception("boom1"));
        //await task2.Invoking(t => t).Should().ThrowExactlyAsync<Exception>().WithMessage("boom1");
        var result2 = await task2;
        result2.IsSuccess.Should().BeFalse();
        result2.Error!.Message.Should().Be("boom1");
        query.LastData.Should().Be("0");

        // Change arg with failure
        var task3 = query.SetArgAsyncInternal(1);
        query.LastData.Should().Be("0");

        qf.GetSource(2).SetException(new Exception("boom2"));
        await task3.Invoking(t => t).Should().ThrowExactlyAsync<Exception>().WithMessage("boom2");
        query.LastData.Should().Be("0");
    }

    [UIFact]
    public void Idle_query_tests()
    {
        var query = new ParameterlessEndpoint<string>(
            _ => ReturnAsync("test")
        ).Use();

        AssertIsIdleState(query);

        // These should be no-ops:
        query.Detach();
        query.Cancel();

        AssertIsIdleState(query);
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
