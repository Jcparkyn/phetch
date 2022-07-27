namespace Phetch.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Xunit;

    public class QueryEndpointTests
    {
        [Fact]
        public async Task Should_create_valid_query()
        {
            var endpoint = new Endpoint<int, string>(
                (val, _) => Task.FromResult(val.ToString())
            );
            var query = endpoint.Use();
            await query.SetArgAsync(10);
            query.Data.Should().Be("10");
        }

        [Fact]
        public async Task Should_share_cache_between_queries()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                (val, _) =>
                {
                    numQueryFnCalls++;
                    return Task.FromResult(val.ToString());
                }
            );
            var options = new QueryOptions<int, string>()
            {
                StaleTime = TimeSpan.FromMinutes(100),
            };
            var query1 = endpoint.Use(options);
            var query2 = endpoint.Use(options);
            await query1.SetArgAsync(10);
            await query2.SetArgAsync(10);

            query1.Data.Should().Be("10");
            query2.Data.Should().Be("10");

            numQueryFnCalls.Should().Be(1);
        }

        [Fact]
        public async Task Invalidate_should_rerun_query()
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
            var query1 = endpoint.Use();
            var query2 = endpoint.Use();

            await query1.SetArgAsync(1);
            await query2.SetArgAsync(2);

            numQueryFnCalls.Should().Be(2);

            endpoint.Invalidate(1);

            query1.IsFetching.Should().BeTrue();
            query2.IsFetching.Should().BeFalse();

            numQueryFnCalls.Should().Be(3);

            endpoint.InvalidateAll();

            numQueryFnCalls.Should().Be(5);
        }

        [Fact]
        public async Task Invoke_should_work()
        {
            var endpoint = new Endpoint<int, string>(
                (val, _) => Task.FromResult(val.ToString())
            );
            var result = await endpoint.Invoke(2);
            result.Should().Be("2");
        }

        [Fact]
        public async Task UpdateQueryData_should_work()
        {
            var endpoint = new Endpoint<int, string>(
                (val, _) => Task.FromResult(val.ToString())
            );
            var query1 = endpoint.Use();
            var query2 = endpoint.Use();

            await query1.SetArgAsync(1);
            await query2.SetArgAsync(2);

            query1.Data.Should().Be("1");
            query2.Data.Should().Be("2");

            endpoint.UpdateQueryData(1, "updated");

            query1.Data.Should().Be("updated");
            query2.Data.Should().Be("2");
        }

        [Fact]
        public async Task Prefetch_should_prefetch_data()
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
        public async Task Prefetch_should_do_nothing_if_query_exists()
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
        public async Task Prefetch_should_refetch_failed_query()
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

        class FakeQueryService
        {
            public int NumQueryFnCalls { get; private set; }
            public async Task<string> Run(int val, CancellationToken ct)
            {
                NumQueryFnCalls++;
                await Task.Delay(1, ct);
                return val.ToString();
            }
        }
    }
}
