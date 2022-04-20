namespace Phetch.Tests
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class QueryObserverTests
    {
        [Fact]
        public async Task Should_work_with_basic_query()
        {
            var query = new Query<string>(
                () => Task.FromResult("test"),
                runAutomatically: false
            );
            await query.SetParamsAsync(default);

            query.Data.Should().Be("test");
            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();
            query.IsError.Should().BeFalse();
            query.Error.Should().BeNull();
        }

        [Fact]
        public async Task Should_set_loading_states_correctly()
        {
            var tcs = new TaskCompletionSource<string>();
            var query = new Query<string>(
                () => tcs.Task,
                runAutomatically: false
            );

            query.Status.Should().Be(QueryStatus.Idle);

            // Fetch once
            var refetchTask = query.SetParamsAsync(default);

            query.IsLoading.Should().BeTrue();
            query.IsFetching.Should().BeTrue();

            await Task.Delay(100);
            tcs.SetResult("test");
            await refetchTask;

            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();

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
        }

        [Fact]
        public async Task Should_handle_query_error()
        {
            var error = new IndexOutOfRangeException("message");
            var query = new Query<string>(
                () => Task.FromException<string>(error),
                runAutomatically: false
            );

            await query.Invoking(x => x.SetParamsAsync(default))
                .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

            query.Data.Should().BeNull();
            query.Status.Should().Be(QueryStatus.Error);
            query.Error.Should().Be(error);

            query.IsError.Should().BeTrue();
            query.IsSuccess.Should().BeFalse();
            query.IsLoading.Should().BeFalse();
        }
    }
}
