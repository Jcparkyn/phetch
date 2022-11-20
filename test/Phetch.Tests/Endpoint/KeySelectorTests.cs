namespace Phetch.Tests.Endpoint;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;

public class KeySelectorTests
{
    [UIFact]
    public async Task Should_use_custom_KeySelector()
    {
        var endpoint = new Endpoint<(int, object), string>(
            arg => TestHelpers.ReturnAsync(arg.Item1.ToString()),
            options: new()
            {
                KeySelector = arg => $"key: {arg.Item1}", // Ignore object from key
                DefaultStaleTime = TimeSpan.MaxValue,
            }
        );
        var query1 = endpoint.Use();
        var result = await query1.SetArgAsync((10, new object()));

        var query2 = endpoint.Use();
        var setArgAgainTask = query2.SetArgAsync((10, new object()));
        setArgAgainTask.IsCompletedSuccessfully.Should().BeTrue();
        (await setArgAgainTask).Should().Be("10");

        var query3 = endpoint.Use();
        await query3.SetArgAsync((11, new object()));

        using (new AssertionScope())
        {
            result.Should().Be("10");
            query1.Data.Should().Be("10");
            query2.Data.Should().Be("10");
            query3.Data.Should().Be("11");
            endpoint.GetCachedQuery((10, new object())).Should().Be(query1.CurrentQuery);
            endpoint.GetCachedQueryByKey("key: 10").Should().Be(query1.CurrentQuery);
        }
    }
}
