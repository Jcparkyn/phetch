namespace Phetch.Tests;

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;

public class OptionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void QueryOptions_should_convert_from_non_generic_options(bool useCtor)
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

        var options2 = useCtor
            ? new QueryOptions<int, string>(options)
            : (QueryOptions<int, string>)options;

        using (new AssertionScope())
        {
            options2.OnSuccess.Should().Be(onSuccess);
            options2.OnFailure.Should().Be(onFailure);
            options2.StaleTime.Should().Be(TimeSpan.FromSeconds(1));
            options2.RetryHandler.Should().Be(RetryHandler.None);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointOptions_should_convert_from_non_generic_options(bool useCtor)
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

        var options2 = useCtor
            ? new EndpointOptions<int, string>(options)
            : options;

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

    [Fact]
    public void Default_options_should_exist()
    {
        QueryOptions.Default.Should().NotBeNull();
        QueryOptions<int, string>.Default.Should().NotBeNull();

        EndpointOptions<int, string>.Default.Should().NotBeNull();
    }
}
