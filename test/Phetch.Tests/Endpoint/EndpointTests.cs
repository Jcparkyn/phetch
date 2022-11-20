namespace Phetch.Tests.Endpoint
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using Phetch.Core;
    using Xunit;

    public class EndpointTests
    {
        [UIFact]
        public async Task Should_create_valid_query()
        {
            var endpoint = new Endpoint<int, string>(
                val => TestHelpers.ReturnAsync(val.ToString())
            );
            var query = endpoint.Use();
            var result = await query.SetArgAsync(10);

            using (new AssertionScope())
            {
                query.Options.Should().Be(QueryOptions<int, string>.Default);
                result.Should().Be("10");
                query.Data.Should().Be("10");
                endpoint.GetCachedQuery(10).Should().Be(query.CurrentQuery);
                endpoint.GetCachedQueryByKey(10).Should().Be(query.CurrentQuery);
            }
        }

        [UIFact]
        public async Task Should_share_cache_between_queries()
        {
            var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
            var endpoint = new Endpoint<int, string>(queryFn);

            var options = new QueryOptions<int, string>()
            {
                StaleTime = TimeSpan.FromMinutes(100),
            };
            var query1 = endpoint.Use(options);
            var query2 = endpoint.Use(options);
            var result1 = await query1.SetArgAsync(10);
            var result2 = await query2.SetArgAsync(10);

            result1.Should().Be("10");
            result2.Should().Be("10");

            query1.Data.Should().Be("10");
            query2.Data.Should().Be("10");

            queryFnCalls.Should().Equal(10);
        }

        [UIFact]
        public async Task Invalidate_should_rerun_query()
        {
            var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
            var endpoint = new Endpoint<int, string>(queryFn);

            var query1 = endpoint.Use();
            var query2 = endpoint.Use();

            await query1.SetArgAsync(1);
            await query2.SetArgAsync(2);

            queryFnCalls.Should().Equal(1, 2);

            endpoint.Invalidate(1);

            query1.IsFetching.Should().BeTrue();
            query2.IsFetching.Should().BeFalse();

            queryFnCalls.Should().Equal(1, 2, 1);

            endpoint.InvalidateAll();

            queryFnCalls.Should().Equal(1, 2, 1, 1, 2);
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

            var result1 = await query1.SetArgAsync(1);
            var result2 = await query2.SetArgAsync(2);
            var result3 = await query3.SetArgAsync(3);

            using (new AssertionScope())
            {
                result1.Should().Be("1");
                result2.Should().Be("2");
                result3.Should().Be("3");
                query1.Data.Should().Be("1");
                query2.Data.Should().Be("2");
                query3.Data.Should().Be("3");
            }
            endpoint.UpdateQueryData(2, "2 - test1");
            endpoint.UpdateQueryData(3, q => q.Data + " - test2");

            using (new AssertionScope())
            {
                query1.Data.Should().Be("1");
                query2.Data.Should().Be("2 - test1");
                query3.Data.Should().Be("3 - test2");
            }
        }

        [UIFact]
        public async Task UpdateQueryData_for_new_arg_should_add_cache_entry()
        {
            var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
            var endpoint = new Endpoint<int, string>(queryFn);

            endpoint.UpdateQueryData(1, "1", true);
            var query1 = endpoint.Use(new()
            {
                StaleTime = TimeSpan.FromSeconds(30),
            });

            var setArgTask = query1.SetArgAsync(1);

            using (new AssertionScope())
            {
                setArgTask.IsCompletedSuccessfully.Should().BeTrue();
                query1.IsSuccess.Should().BeTrue();
                query1.Data.Should().Be("1");
                queryFnCalls.Should().BeEmpty();

                var result = await setArgTask;
                result.Should().Be("1");
            }
        }

        [UIFact]
        public async Task Should_handle_null_value_keys()
        {
            var endpoint = new Endpoint<int?, string>(
                val => TestHelpers.ReturnAsync(val?.ToString() ?? "null")
            );
            var query1 = endpoint.Use();
            var result1 = await query1.SetArgAsync(10);
            var queryNull = endpoint.Use();
            var resultNull = await queryNull.SetArgAsync(null);

            using (new AssertionScope())
            {
                result1.Should().Be("10");
                query1.Data.Should().Be("10");
                resultNull.Should().Be("null");
                queryNull.Data.Should().Be("null");

                endpoint.GetCachedQuery(10).Should().Be(query1.CurrentQuery);
                endpoint.GetCachedQuery(null).Should().Be(queryNull.CurrentQuery);
                endpoint.TryGetCachedResult(10, out var data).Should().BeTrue();
                data.Should().Be("10");
            }
        }

        [UIFact]
        public async Task Should_handle_null_reference_keys()
        {
            var endpoint = new Endpoint<string?, string>(
                val => TestHelpers.ReturnAsync(val?.ToString() ?? "null")
            );
            var query1 = endpoint.Use();
            var result1 = await query1.SetArgAsync("10");
            var queryNull = endpoint.Use();
            var resultNull = await queryNull.SetArgAsync(null);

            using (new AssertionScope())
            {
                result1.Should().Be("10");
                query1.Data.Should().Be("10");
                resultNull.Should().Be("null");
                queryNull.Data.Should().Be("null");

                endpoint.GetCachedQuery("10").Should().Be(query1.CurrentQuery);
                endpoint.GetCachedQuery(null).Should().Be(queryNull.CurrentQuery);
                endpoint.TryGetCachedResult("10", out var data).Should().BeTrue();
                data.Should().Be("10");
            }
        }
    }
}
