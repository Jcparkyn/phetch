namespace Phetch.Tests.Endpoint
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Polly;
    using Xunit;

    public class RetryHandlerTests
    {
        [UITheory]
        [MemberData(nameof(GetRetryHandlers), 0)]
        [MemberData(nameof(GetRetryHandlers), 1)]
        [MemberData(nameof(GetRetryHandlers), 2)]
        public async Task Should_succeed_with_handler_if_query_succeeds(IRetryHandler retryHandler)
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(1);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = retryHandler,
            });

            var query = endpoint.Use();
            var setArgTask = query.SetArgAsync(1);
            sources[0].SetResult("1");

            await setArgTask;

            query.IsSuccess.Should().BeTrue();
            query.Data.Should().Be("1");
            queryFnCalls.Should().Equal(1);
        }

        [UITheory]
        [MemberData(nameof(GetRetryHandlers), 1)]
        [MemberData(nameof(GetRetryHandlers), 2)]
        public async Task Should_retry_with_handler(IRetryHandler retryHandler)
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(2);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = retryHandler,
            });

            var query = endpoint.Use();
            var setArgTask = query.SetArgAsync(1);
            sources[0].SetException(new HttpRequestException("fail 1"));
            sources[1].SetResult("1");

            await setArgTask;

            query.IsSuccess.Should().BeTrue();
            query.Data.Should().Be("1");
            queryFnCalls.Should().Equal(1, 1);
        }

        [UITheory]
        [MemberData(nameof(GetRetryHandlers), 1)]
        public async Task Should_fail_with_handler_if_retry_fails(IRetryHandler retryHandler)
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(2);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = retryHandler,
            });

            var query = endpoint.Use();
            var setArgTask = query.Invoking(q => q.SetArgAsync(1))
                .Should().ThrowExactlyAsync<HttpRequestException>().WithMessage("fail 2");

            sources[0].SetException(new HttpRequestException("fail 1"));
            var ex2 = new HttpRequestException("fail 2");
            sources[1].SetException(ex2);

            await setArgTask;

            query.IsSuccess.Should().BeFalse();
            query.IsError.Should().BeTrue();
            query.Error.Should().Be(ex2);
            queryFnCalls.Should().Equal(1, 1);
        }

        [UIFact]
        public async Task Should_work_with_NoRetryHandler()
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(1);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = RetryHandler.None,
            });

            var query = endpoint.Use();
            var setArgTask = query.SetArgAsync(1);

            sources[0].SetResult("1");

            await setArgTask;

            query.IsSuccess.Should().BeTrue();
            query.Data.Should().Be("1");
            queryFnCalls.Should().Equal(1);
        }

        [UITheory]
        [MemberData(nameof(GetRetryHandlers), 1)]
        public async Task Should_use_query_RetryHandler_if_passed(IRetryHandler retryHandler)
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(2);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = RetryHandler.None,
            });

            var query = endpoint.Use(new()
            {
                RetryHandler = retryHandler,
            });

            var setArgTask = query.SetArgAsync(1);
            sources[0].SetException(new HttpRequestException("fail 1"));
            sources[1].SetResult("1");

            await setArgTask;

            query.IsSuccess.Should().BeTrue();
            query.Data.Should().Be("1");
            queryFnCalls.Should().Equal(1, 1);
        }

        [UIFact]
        public async Task Should_have_no_retry_if_RetryHandler_None_is_used_in_query()
        {
            var (queryFn, sources, queryFnCalls) = TestHelpers.MakeCustomTrackedQueryFn(1);
            var endpoint = new Endpoint<int, string>(queryFn, new()
            {
                RetryHandler = RetryHandler.Simple(1),
            });

            var query = endpoint.Use(new()
            {
                RetryHandler = RetryHandler.None,
            });
            var setArgTask = query.Invoking(q => q.SetArgAsync(1))
                .Should().ThrowExactlyAsync<HttpRequestException>().WithMessage("fail 1");

            var ex = new HttpRequestException("fail 1");
            sources[0].SetException(ex);

            await setArgTask;

            query.IsSuccess.Should().BeFalse();
            query.IsError.Should().BeTrue();
            query.Error.Should().Be(ex);
            queryFnCalls.Should().Equal(1);
        }

        public static IEnumerable<IRetryHandler[]> GetRetryHandlers(int retryCount)
        {
            yield return new[] {
                new PollyRetryHandler(Policy
                    .Handle<HttpRequestException>()
                    .RetryAsync(retryCount))
            };

            yield return new[] {
                RetryHandler.Simple(retryCount)
            };
        }
    }
}
