namespace Phetch.Tests;

using FluentAssertions;
using Phetch.Core;
using System;
using System.Threading.Tasks;
using Xunit;

public class QueryResultTests
{
    [Fact]
    public void SuccessConstructor()
    {
        var result = new QueryResult<string>("123");
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().Be("123");

        result.GetOrThrow().Should().Be("123");
        result.GetOrDefault("default").Should().Be("123");
    }

    [Fact]
    public void FailureConstructor()
    {
        var error = new IndexOutOfRangeException("boom");
        var result = new QueryResult<string>(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Data.Should().BeNull();

        result.Invoking(r => r.GetOrThrow()).Should().Throw<IndexOutOfRangeException>();
        result.GetOrDefault("default").Should().Be("default");
    }

    [Fact]
    public async Task OfAsync_success()
    {
        var result = await QueryResult.OfAsync(() => TestHelpers.ReturnAsync("123"));
        result.Data.Should().Be("123");
    }

    [Fact]
    public async Task OfAsync_failure()
    {
        var result = await QueryResult.OfAsync<string>(async () =>
        {
            await Task.Yield();
            throw new IndexOutOfRangeException("boom");
        });
        result.Error!.Message.Should().Be("boom");
    }
}
