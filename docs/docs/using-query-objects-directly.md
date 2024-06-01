# Using Query objects directly

In cases where the `<UseEndpoint/>` component doesn't provide enough control, you can also use Query objects directly in your code.
This is also useful when using endpoints or queries inside DI services.

`Phetch.Blazor` includes the `<ObserveQuery/>` component for this purpose, so that components can automatically be re-rendered when the query state changes.

> :information_source: Alternatively, you can manually subscribe and un-subscribe to a query using the `StateChanged` event.

```cshtml
@implements IDisposable
@inject MyApi Api

@{ query.SetArg(ThingId); }

<ObserveQuery Target="query">
    @* Put content that depends on the query here. *@
    @if (query.HasData)
    {
        // etc...
    }
</ObserveQuery>

@code {
    private Query<int, Thing> query = null!;
    [Parameter] public int ThingId { get; set; }

    protected override void OnInitialized()
    {
        query = Api.GetThing.Use();
    }

    // Disposing the query signals to the cache that the result is no longer being used, and avoids memory leaks.
    public void Dispose() => query.Dispose();
}
```

Content outside of the `<ObserveQuery/>` component will not be re-rendered when the query state changes. If you need the whole component to re-render, you can call `StateHasChanged` without adding child content:

```cshtml
<ObserveQuery Target="query" OnChanged="StateHasChanged"/>
```