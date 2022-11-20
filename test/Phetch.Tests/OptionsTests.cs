namespace Phetch.Tests;

using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using System;
using Xunit;

public class OptionsTests
{
    [Fact]
    public void QueryOptions_should_convert_from_non_generic_options()
    {
        var onSuccess = (EventArgs e) => { };
        var onFailure = (QueryFailureEventArgs e) => { };
        var options = new QueryOptions()
        {
            StaleTime = TimeSpan.FromSeconds(1),
            RetryHandler = RetryHandler.None,
            OnSuccess = onSuccess,
            OnFailure = onFailure,
        };

        var options2 = new QueryOptions<int, string>(options);

        using (new AssertionScope())
        {
            options2.OnSuccess.Should().Be(onSuccess);
            options2.OnFailure.Should().Be(onFailure);
            options2.StaleTime.Should().Be(TimeSpan.FromSeconds(1));
            options2.RetryHandler.Should().Be(RetryHandler.None);
        }
    }

    [Fact]
    public void EndpointOptions_should_convert_from_non_generic_options()
    {
        var onSuccess = (EventArgs e) => { };
        var onFailure = (QueryFailureEventArgs e) => { };

        var options = new EndpointOptions()
        {
            DefaultStaleTime = TimeSpan.FromSeconds(1),
            RetryHandler = RetryHandler.None,
            OnSuccess = onSuccess,
            OnFailure = onFailure,
            CacheTime = TimeSpan.FromSeconds(2),
        };

        var options2 = new EndpointOptions<int, string>(options);

        using (new AssertionScope())
        {
            options2.OnSuccess.Should().Be(onSuccess);
            options2.OnFailure.Should().Be(onFailure);
            options2.DefaultStaleTime.Should().Be(TimeSpan.FromSeconds(1));
            options2.RetryHandler.Should().Be(RetryHandler.None);
            options2.KeySelector.Should().BeNull();
            options2.CacheTime.Should().Be(TimeSpan.FromSeconds(2));
        }
    }
}
