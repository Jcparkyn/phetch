namespace BlazorQuery.Tests
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class QueryTests
    {
        [Fact]
        public async Task Should_work_with_basic_query()
        {
            var query = new Query<string>(
                null,
                token => Task.FromResult("test"),
                runAutomatically: false
            );
            var result = await query.RefetchAsync();

            result.Should().Be("test");
            query.Data.Should().Be("test");
            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.Error.Should().BeNull();
        }

        [Fact]
        public async Task Should_handle_query_error()
        {
            var error = new IndexOutOfRangeException("message");
            var query = new Query<string>(
                null,
                token => Task.FromException<string>(error),
                runAutomatically: false
            );

            await query.Invoking(x => x.RefetchAsync())
                .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

            query.Data.Should().BeNull();
            query.Status.Should().Be(QueryStatus.Error);
            query.Error.Should().Be(error);

            query.IsError.Should().BeTrue();
            query.IsSuccess.Should().BeFalse();
            query.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task Should_cancel_running_query()
        {
            var query = new Query<string>(
                null,
                async token =>
                {
                    await Task.Delay(10000, token);
                    return "test"; // This should never be reached
                },
                runAutomatically: false
            );

            var firstTask = query.Awaiting(q => q.RefetchAsync())
                .Should().ThrowAsync<TaskCanceledException>();

            await Task.Yield();

            query.Refetch();

            await firstTask;

            query.Data.Should().BeNull();
            query.Status.Should().Be(QueryStatus.Loading);
        }
    }
}
