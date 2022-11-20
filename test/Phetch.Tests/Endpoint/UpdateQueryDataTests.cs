namespace Phetch.Tests.Endpoint;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Core;
using Xunit;

public class UpdateQueryDataTests
{
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
    public async Task UpdateQueryData_should_affect_triggered_query()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString())
        );
        var query1 = endpoint.Use();
        var query2 = endpoint.Use();

        var result1 = await query1.TriggerAsync(1);
        var result2 = await query2.TriggerAsync(2);

        using (new AssertionScope())
        {
            result1.Should().Be("1");
            result2.Should().Be("2");
            query1.Data.Should().Be("1");
            query2.Data.Should().Be("2");
        }
        endpoint.UpdateQueryData(1, "1 - test1");

        using (new AssertionScope())
        {
            query1.Data.Should().Be("1 - test1");
            query2.Data.Should().Be("2");
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
}
