namespace Phetch.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Xunit;

    public class MutationTests
    {
        [Fact]
        public async Task Should_set_loading_states_correctly()
        {
            var tcs = new TaskCompletionSource<int>();
            var mut = new Mutation<int, int>(
                val => tcs.Task
            );

            mut.Status.Should().Be(QueryStatus.Idle);
            mut.IsUninitialized.Should().BeTrue();
            mut.HasData.Should().BeFalse();

            // Fetch once
            var triggerTask1 = mut.TriggerAsync(10);

            mut.IsLoading.Should().BeTrue();

            await Task.Delay(1);
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

            tcs.SetResult(21);
            var result2 = await triggerTask2;

            result2.Should().Be(21);
            mut.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task Should_handle_query_error()
        {
            var error = new IndexOutOfRangeException("message");
            var query = new Mutation<string>(
                _ => Task.FromException(error)
            );

            await query.Invoking(x => x.TriggerAsync("test"))
                .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

            query.HasData.Should().BeFalse();
            query.Status.Should().Be(QueryStatus.Error);
            query.Error.Should().Be(error);

            query.IsError.Should().BeTrue();
            query.IsSuccess.Should().BeFalse();
            query.IsLoading.Should().BeFalse();
        }
    }
}
