namespace Phetch.Tests.Endpoint;

using System.Threading.Tasks;
using FluentAssertions;
using Phetch.Core;
using Xunit;

public class ResultlessEndpointTests
{
    [UIFact]
    public async Task ResultlessEndpoint_should_work()
    {
        var (queryFn, queryFnCalls) = TestHelpers.MakeTrackedQueryFn();
        var endpoint = new ResultlessEndpoint<int>(queryFn);
        var mut = endpoint.Use();

        mut.IsUninitialized.Should().BeTrue();
        mut.Status.Should().Be(QueryStatus.Idle);

        await mut.TriggerAsync(10);

        mut.Status.Should().Be(QueryStatus.Success);
        queryFnCalls.Should().Equal(10);
    }
}
