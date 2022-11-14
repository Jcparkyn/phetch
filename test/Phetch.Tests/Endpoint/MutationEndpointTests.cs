namespace Phetch.Tests.Endpoint
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Phetch.Core;
    using Xunit;

    public class MutationEndpointTests
    {
        [UIFact]
        public async Task MutationEndpoint_should_work()
        {
            var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
            var endpoint = new MutationEndpoint<int>(queryFn);
            var mut = endpoint.Use();

            mut.IsUninitialized.Should().BeTrue();
            mut.Status.Should().Be(QueryStatus.Idle);

            await mut.TriggerAsync(10);

            mut.Status.Should().Be(QueryStatus.Success);
            queryFnCalls.Should().Equal(10);
        }
    }
}
