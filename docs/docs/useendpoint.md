# Using Query Endpoints with `<UseEndpoint/>`

Once you've defined a query endpoint, the best way to use it (in most cases) is with the `<UseEndpoint/>` Blazor component. This will handle re-rending the component automatically when the data changes.

If you provide the `Arg` parameter (the value to pass to the endpoint), this will also automatically fetch the data, and request new data when the argument changes. Without an `Arg` parameter (or with `AutoFetch="false"`), the data will not be fetched automatically.

> :information_source: With `<UseParameterlessEndpoint/>`, you don't need to supply an `Arg`.
If you don't want the query to be fetched automatically, you can set `AutoFetch="false"`.

```cshtml
@*This assumes you've created a class called MyApi containing your endpoints, and
  registered it as a singleton or scoped service for dependency injection. *@
@inject MyApi Api

<UseEndpoint Endpoint="@Api.GetThing" Arg="ThingId" Context="query">
    @if (query.HasData)
    {
        <p>Thing Name: @query.Data.Name</p>
    }
    else if (query.IsLoading)
    {
        <p>Loading...</p>
    }
    else if (query.IsError)
    {
        <p>Error: @query.Error.Message</p>
    }
</UseEndpoint>

@code {
    [Parameter] public int ThingId { get; set; }
}
```

For a full working example, view the [sample project](https://github.com/jcparkyn/phetch/blob/main/samples/HackerNewsClient/Pages/PostDetails.razor).