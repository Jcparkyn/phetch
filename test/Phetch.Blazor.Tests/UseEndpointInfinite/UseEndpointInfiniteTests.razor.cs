namespace Phetch.Blazor.Tests.UseEndpointInfinite;

using Bunit.Rendering;
using FluentAssertions;
using FluentAssertions.Execution;
using Phetch.Blazor.Experimental;
using Phetch.Core;
using Phetch.Tests;
using System;
using System.Threading.Tasks;

public partial class UseEndpointInfiniteTests : TestContext
{
    [Fact]
    public void Should_render_child_content()
    {
        var endpoint = new Endpoint<int, string>(
            val => TestHelpers.ReturnAsync(val.ToString())
        );
        var cut = RenderComponent<UseEndpointInfinite<int, string>>(parameters => parameters
            .Add(p => p.Endpoint, endpoint)
            .Add(p => p.GetNextPageArg, pages => (pages.Count + 1, true))
            .Add(p => p.Arg, 1)
            .Add(p => p.ChildContent, p => "Hello, world!")
        );
        cut.MarkupMatches("Hello, world!");
    }

    [Fact]
    public async Task Basic_usage()
    {
        var qf = new MockQueryFunction<int, PageResponse>(2);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query);
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        // Initial render (while loading)
        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.Should().NotBeNull();
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeTrue();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsLoading.Should().BeTrue();
                page.Arg.Should().Be(0);
            });
        }

        await cut.InvokeAsync(() => qf.SetResult(0, new PageResponse(2)));

        // Loaded first page
        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeTrue();
            ctx.IsLoadingNextPage.Should().BeFalse();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsSuccess.Should().BeTrue();
                page.Data.Should().Be(new PageResponse(2));
            });
        }

        // Explicit <Task> to ensure we don't wait for LoadNextPageAsync to complete
        var loadTask = await cut.InvokeAsync<Task<PageResponse>>(content.Instance.Context.LoadNextPageAsync);

        // Loading second and final page
        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeTrue();
            ctx.Pages.Should().SatisfyRespectively(
                page =>
                {
                    page.IsSuccess.Should().BeTrue();
                    page.Data.Should().Be(new PageResponse(2));
                },
                page =>
                {
                    page.IsLoading.Should().BeTrue();
                    page.Arg.Should().Be(2);
                });
        }
        await cut.InvokeAsync(() => qf.SetResult(1, new PageResponse(null)));
        var loadTaskResult = await loadTask;
        loadTaskResult.Should().Be(new PageResponse(null));

        // Loaded final page
        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeFalse();
            ctx.Pages[1].IsSuccess.Should().BeTrue();
            ctx.Pages[1].Data.Should().Be(new PageResponse(null));

            // LoadNextPageAsync should fail
            await content.Instance.Context.Invoking(c => c.LoadNextPageAsync()).Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot load next page because GetNextPageArg returned hasNextPage=false");
            // LoadNextPage should swallow error
            content.Instance.Context.Invoking(c => c.LoadNextPage()).Should().NotThrow();
        }
    }

    [Fact]
    public async Task Error_on_first_page()
    {
        var qf = new MockQueryFunction<int, PageResponse>(2);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query);
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        var ex = new ArgumentOutOfRangeException("boom");
        await cut.InvokeAsync(() => qf.Sources[0].SetException(ex));

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeFalse();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsError.Should().BeTrue();
                page.Error.Should().Be(ex);
            });
            // LoadNextPageAsync should fail
            await content.Instance.Context.Invoking(c => c.LoadNextPageAsync()).Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot load next page because last page hasn't succeeded");
        }


        // Retry first page
        // Explicit <Task> to ensure we don't wait for RefetchAsync to complete
        await cut.InvokeAsync<Task>(content.Instance.Context.Pages[0].RefetchAsync);

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeTrue();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsLoading.Should().BeTrue();
            });
        }

        // Successful first page
        await cut.InvokeAsync(() => qf.SetResult(1, new PageResponse(2)));

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeTrue();
            ctx.IsLoadingNextPage.Should().BeFalse();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsSuccess.Should().BeTrue();
                page.Data.Should().Be(new PageResponse(2));
            });
        }
    }

    [Fact]
    public async Task Should_refetch_when_Arg_changes_single_page()
    {
        var qf = new MockQueryFunction<int, PageResponse>(2);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query);
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        await cut.InvokeAsync(() => qf.SetResult(0, new PageResponse(null)));

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Arg, 7)
        );

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeTrue();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsLoading.Should().BeTrue();
                page.Arg.Should().Be(7);
            });
        }

        await cut.InvokeAsync(() => qf.SetResult(1, new PageResponse(null)));

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsSuccess.Should().BeTrue();
            });
            qf.Calls.Should().Equal(0, 7);
        }
    }

    [Fact]
    public async Task Should_refetch_when_Arg_changes_multiple_pages()
    {
        var qf = new MockQueryFunction<int, PageResponse>(3);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query);
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        // Load first two pages
        await cut.InvokeAsync(() => qf.SetResult(0, new PageResponse(2)));
        await cut.InvokeAsync<Task>(content.Instance.Context.LoadNextPageAsync);
        await cut.InvokeAsync(() => qf.SetResult(1, new PageResponse(null)));

        content.Instance.Context.Pages.Should().HaveCount(2);

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Arg, 7)
        );

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeTrue();
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsLoading.Should().BeTrue();
                page.Arg.Should().Be(7);
            });
        }

        await cut.InvokeAsync(() => qf.SetResult(2, new PageResponse(null)));

        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.Pages.Should().SatisfyRespectively(page =>
            {
                page.IsSuccess.Should().BeTrue();
            });
            qf.Calls.Should().Equal(0, 2, 7);
        }
    }

    [Fact]
    public async Task Should_refetch_when_Arg_changes_precached()
    {
        var qf = new MockQueryFunction<int, PageResponse>(1);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query, new()
        {
            DefaultStaleTime = TimeSpan.MaxValue,
        });
        endpoint.UpdateQueryData(7, new PageResponse(9), addIfNotExists: true);
        endpoint.UpdateQueryData(9, new PageResponse(null), addIfNotExists: true);
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        await cut.InvokeAsync(() => qf.SetResult(0, new PageResponse(null)));

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Arg, 7)
        );

        // It should detect that the first two pages are in the cache and load them immediately.
        using (new AssertionScope())
        {
            var ctx = content.Instance.Context;
            ctx.HasNextPage.Should().BeFalse();
            ctx.IsLoadingNextPage.Should().BeFalse();
            ctx.Pages.Should().SatisfyRespectively(
                page =>
                {
                    page.IsSuccess.Should().BeTrue();
                    page.Data.Should().Be(new PageResponse(9));
                },
                page =>
                {
                    page.IsSuccess.Should().BeTrue();
                    page.Arg.Should().Be(9);
                    page.Data.Should().Be(new PageResponse(null));
                }
            );
            qf.Calls.Should().Equal(0);
        }
    }

    [Fact]
    public async Task Should_dispose_Query_when_disposed()
    {
        var qf = new MockQueryFunction<int, PageResponse>(1);
        var endpoint = new Endpoint<int, PageResponse>(qf.Query, options: new()
        {
            CacheTime = TimeSpan.Zero,
        });
        using var cut = Render<UseEndpointInfinite<int, PageResponse>>(EndpointFragment1(endpoint, arg: 0));
        var content = cut.FindComponent<UseEndpointInfiniteTestContent>();

        await cut.InvokeAsync(() => qf.SetResult(0, new PageResponse(2)));

        endpoint.GetCachedQuery(0).Should().NotBeNull();

        DisposeComponents();
        endpoint.GetCachedQuery(0).Should().BeNull();
    }
}

static class RenderedFragmentExtensions
{
    /// Version of
    /// <see cref="RenderedFragmentInvokeAsyncExtensions.InvokeAsync(IRenderedFragmentBase, Action)"/>
    /// with a return value.
    public static Task<T> InvokeAsync<T>(this IRenderedFragmentBase renderedFragment, Func<T> workItem)
    {
        ArgumentNullException.ThrowIfNull(renderedFragment);

        return renderedFragment.Services.GetRequiredService<ITestRenderer>()
            .Dispatcher.InvokeAsync(workItem);
    }
}
