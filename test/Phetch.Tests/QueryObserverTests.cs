namespace Phetch.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
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
            await query.SetParamAsync(default);

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
            query.IsUninitialized.Should().BeTrue();
            query.HasData.Should().BeFalse();

            // Fetch once
            var refetchTask = query.SetParamAsync(default);

            query.IsLoading.Should().BeTrue();
            query.IsFetching.Should().BeTrue();

            await Task.Delay(1);
            tcs.SetResult("test");
            await refetchTask;

            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();
            query.HasData.Should().BeTrue();

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

            await query.Invoking(x => x.SetParamAsync(default))
                .Should().ThrowExactlyAsync<IndexOutOfRangeException>();

            query.Data.Should().BeNull();
            query.Status.Should().Be(QueryStatus.Error);
            query.Error.Should().Be(error);

            query.IsError.Should().BeTrue();
            query.IsSuccess.Should().BeFalse();
            query.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task Should_always_keep_most_recent_data()
        {
            // Timing:
            // t0 -------- [keep]
            // t1     --------- [keep]
            //        ^ refetch

            var (queryFn, sources) = MakeCustomQueryFn(2);
            var query = new Query<string>(
                queryFn,
                runAutomatically: false
            );

            query.Status.Should().Be(QueryStatus.Idle);

            query.SetParam(default);

            query.IsLoading.Should().BeTrue();
            query.IsFetching.Should().BeTrue();

            query.Refetch();

            sources[0].SetResult("test0");
            await Task.Delay(1);

            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeTrue();
            query.Data.Should().Be("test0");

            sources[1].SetResult("test1");
            await Task.Delay(1);

            query.Status.Should().Be(QueryStatus.Success);
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();
            query.Data.Should().Be("test1");
        }

        [Fact]
        public async Task Should_ignore_outdated_data()
        {
            // Timing:
            // t0 ------------------- [ignore]
            // t1     --------- [keep]
            //        ^ refetch

            var (queryFn, sources) = MakeCustomQueryFn(2);
            var query = new Query<string>(
                queryFn,
                runAutomatically: false
            );

            query.Status.Should().Be(QueryStatus.Idle);

            query.SetParam(default);

            query.IsLoading.Should().BeTrue();
            query.IsFetching.Should().BeTrue();

            query.Refetch();

            sources[1].SetResult("test1");
            await Task.Delay(1);

            query.Status.Should().Be(QueryStatus.Success);
            query.IsSuccess.Should().BeTrue();
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();
            query.Data.Should().Be("test1");

            sources[0].SetResult("test0");
            await Task.Delay(1);

            query.Status.Should().Be(QueryStatus.Success);
            query.IsLoading.Should().BeFalse();
            query.IsFetching.Should().BeFalse();
            query.Data.Should().Be("test1");
        }

        // Makes a query function that can be called multiple times, using a different TaskCompletionSource each time.
        private static (Func<Task<string>> queryFn, List<TaskCompletionSource<string>> sources) MakeCustomQueryFn(int numSources)
        {
            var sources = Enumerable.Range(0, numSources)
                .Select(_ => new TaskCompletionSource<string>())
                .ToList();

            var queryCount = 0;
            var queryFn = async () =>
            {
                if (queryCount > numSources)
                    throw new Exception("Query function called too many times");
                var resultTask = sources[queryCount].Task;
                queryCount++;
                return await resultTask;
            };
            return (queryFn, sources);
        }
    }
}
