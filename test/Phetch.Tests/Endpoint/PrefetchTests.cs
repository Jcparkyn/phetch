namespace Phetch.Tests.Endpoint
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Xunit;

    public class PrefetchTests
    {
        [Fact]
        public async Task Should_prefetch_data()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                async (val, ct) =>
                {
                    numQueryFnCalls++;
                    await Task.Delay(1, ct);
                    return val.ToString();
                }
            );
            await endpoint.PrefetchAsync(1);

            numQueryFnCalls.Should().Be(1);

            var query = endpoint.Use(new()
            {
                StaleTime = TimeSpan.FromSeconds(10),
            });
            var setArgTask = query.SetArgAsync(1);

            // The query should have completed synchronously without re-fetching
            query.IsSuccess.Should().BeTrue();
            query.Data.Should().Be("1");
            numQueryFnCalls.Should().Be(1);

            await setArgTask;
        }

        [Fact]
        public async Task Should_do_nothing_if_query_exists()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                async (val, ct) =>
                {
                    numQueryFnCalls++;
                    await Task.Delay(1, ct);
                    return val.ToString();
                }
            );
            // Query in progress:
            var query1 = endpoint.Use();
            var setArgTask1 = query1.SetArgAsync(1);
            await endpoint.PrefetchAsync(1);
            await setArgTask1;

            query1.Data.Should().Be("1");
            numQueryFnCalls.Should().Be(1);

            // Complete query:
            var query2 = endpoint.Use();
            await query2.SetArgAsync(1);
            await endpoint.PrefetchAsync(1);

            query2.Data.Should().Be("1");
            numQueryFnCalls.Should().Be(2);
        }

        [Fact]
        public async Task Should_refetch_failed_query()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                async (val, ct) =>
                {
                    numQueryFnCalls++;
                    await Task.Delay(1, ct);
                    throw new Exception("Test exception");
                }
            );

            var query = endpoint.Use(new()
            {
                StaleTime = TimeSpan.FromSeconds(10),
            });

            await query.Awaiting(q => q.SetArgAsync(1)).Should().ThrowAsync<Exception>();
            var prefetchTask = endpoint.Awaiting(e => e.PrefetchAsync(1)).Should().ThrowAsync<Exception>();

            numQueryFnCalls.Should().Be(2);
            query.IsFetching.Should().BeTrue();
            await prefetchTask;
            query.IsError.Should().BeTrue();
        }
    }
}
