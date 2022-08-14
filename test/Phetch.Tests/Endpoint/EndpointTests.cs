namespace Phetch.Tests.Endpoint
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Xunit;

    public class EndpointTests
    {
        [Fact]
        public async Task Should_create_valid_query()
        {
            var endpoint = new Endpoint<int, string>(
                val => TestHelpers.ReturnAsync(val.ToString())
            );
            var query = endpoint.Use();
            await query.SetArgAsync(10);
            query.Data.Should().Be("10");
        }

        [UIFact]
        public async Task Should_share_cache_between_queries()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                async val =>
                {
                    numQueryFnCalls++;
                    await Task.Yield();
                    return val.ToString();
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

        [UIFact]
        public async Task Invalidate_should_rerun_query()
        {
            var numQueryFnCalls = 0;
            var endpoint = new Endpoint<int, string>(
                async val =>
                {
                    numQueryFnCalls++;
                    await Task.Yield();
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

        [UIFact]
        public async Task Invoke_should_work()
        {
            var endpoint = new Endpoint<int, string>(
                val => TestHelpers.ReturnAsync(val.ToString())
            );
            var result = await endpoint.Invoke(2);
            result.Should().Be("2");
        }

        [UIFact]
        public async Task UpdateQueryData_should_work()
        {
            var endpoint = new Endpoint<int, string>(
                val => TestHelpers.ReturnAsync(val.ToString())
            );
            var query1 = endpoint.Use();
            var query2 = endpoint.Use();
            var query3 = endpoint.Use();

            await query1.SetArgAsync(1);
            await query2.SetArgAsync(2);
            await query3.SetArgAsync(3);

            query1.Data.Should().Be("1");
            query2.Data.Should().Be("2");
            query3.Data.Should().Be("3");

            endpoint.UpdateQueryData(2, "2 - test1");
            endpoint.UpdateQueryData(3, q => q.Data + " - test2");

            query1.Data.Should().Be("1");
            query2.Data.Should().Be("2 - test1");
            query3.Data.Should().Be("3 - test2");
        }
    }
}
