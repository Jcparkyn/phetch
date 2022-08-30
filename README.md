# Phetch

Phetch is a small Blazor library for handling async query state, in the style of [React Query](https://github.com/tannerlinsley/react-query), [SWR](https://github.com/vercel/swr), or [RTK Query](https://redux-toolkit.js.org/rtk-query/overview).

Currently, Phetch is only designed for use with Blazor WebAssembly. However, the core package (Phetch.Core) has no dependencies on Blazor or ASP.NET Core, so in theory it can be used anywhere that supports .NET Standard 2.1.

| :warning: Note: Phetch is in early development and likely to change. |
|:---------------------------------------------------------------------|

## Features
- Automatically handles loading and error states, and updates your components whenever the state changes
- Automatically caches data returned by queries, and makes it easy to invalidate or update this cached data when needed.
- Supports any async method as a query or mutation (not restricted just to HTTP requests)
- Built-in support for CancellationTokens
- Supports mutations, dependent queries, pagination, prefetching, and request de-duplication
- 100% strongly typed, with nullability annotations
- Super lightweight and easy to mix-and-match with other state management methods
- No Javascript whatsoever!

## Show me some code!

[Click here to view the source code for the sample project, with more detailed examples.](https://github.com/Jcparkyn/Phetch/tree/main/samples/PhetchBlazorDemo)

Below is the code for a basic component that runs a query when the component is first loaded.
Phetch can do a whole lot more than that though, so make sure to check out the samples project and full documentation!

**Defining an endpoint:**
```cs
using Phetch.Core;

// This defines an endpoint that takes an int and returns a bool.
var isEvenEndpoint = new Endpoint<int, bool>(
    // Replace this part with your own async function:
    async (value, cancellationToken) =>
    {
        var response = await httpClient.GetFromJsonAsync<dynamic>(
            $"https://api.isevenapi.xyz/api/iseven/{value}",
            cancellationToken);
        return response.IsEven;
    }
);
```

**Using the endpoint in a component:**
```cshtml
@using Phetch.Blazor

<UseEndpoint Endpoint="isEvenEndpoint" Arg="3" Context="query">
    @if (query.IsError) {
        <p><em>Something went wrong!</em></p>
    } else if (query.IsLoading) {
        <p><em>Loading...</em></p>
    } else if (query.HasData) {
        <b>The number is @(query.Data ? "even" : "odd")</b>
    }
</UseEndpoint>
```

Some notes on the example above:
- Inside the `<UseEndpoint>` component, you can use `query` to access the current state of the query. Changing the `Context` parameter will rename this object.
- By changing the `Arg` parameter, the query will automatically be re-fetched when needed.
- Normally, you would share endpoints around your application using dependency injection (see [Defining Query Endpoints](#defining-query-endpoints-recommended)).
- If you need to access the query state inside the `@code` block of a component, you can replace `<UseEndpoint/>` with the pattern described in [Using Query Objects Directly](#using-query-objects-directly).

## Installing

You can install Phetch via the .NET CLI with the following command:

```sh
dotnet add package Phetch.Blazor
```

If you're using Visual Studio, you can also install via the built-in NuGet package manager.

## Contributing

Any contributions are welcome, but ideally start by creating an [issue](https://github.com/Jcparkyn/phetch/issues).

## Comparison with other libraries

- **[Fluxor](https://github.com/mrpmorris/Fluxor), [Blazor-State](https://github.com/TimeWarpEngineering/blazor-state) or [Cortex.Net](https://github.com/jspuij/Cortex.Net):** These are general-purpose state management libraries, so:
  - They will give you more control over exactly how your state is updated, and will work for managing non-query state.
  - However, for managing query state, you will need **much** more code to achieve the same things that Phetch can do in just a couple of lines. This becomes particularly important if you need to cache data or share queries across components.
- **[Fusion](https://github.com/servicetitan/Stl.Fusion)**: This is a **much** larger library, focused on real-time updates. Fusion is a game-changer if your app has lots of real-time functionality, but for most applications it will probably be overkill compared to Phetch.

## Usage

There are a few different ways to define and use queries, depending on your use case.

### Defining Query Endpoints (Recommended)

In most cases, the best way to define queries is to use the `Endpoint` class.
All components that use the same endpoint will share the same cache automatically.

```cs
// This defines an endpoint that takes an int and returns a bool.
var isEvenEndpoint = new Endpoint<int, bool>(
    // Replace this part with your own async function:
    async (value, cancellationToken) =>
    {
        var response = await httpClient.GetFromJsonAsync<dynamic>(
            $"https://api.isevenapi.xyz/api/iseven/{value}",
            cancellationToken);
        return response.IsEven;
    }
);
```

You can then share this instance of Endpoint across your whole application and use it wherever you need it.
In most cases, the best way to do this is with Blazor's built-in [dependency injection](https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection).
You can view the [sample project](./samples/HackerNewsClient/Shared/HackerNewsApi.cs) for a full example of how to do this, or follow the steps below:

<details>
<summary>Setting up dependency injection (DI)</summary>

1. Create a class containing an instance of `Endpoint`. You can have as many or few endpoints in a class as you want.
```cs
public class MyApi
{
    // An endpoint to retrieve a thing based on its ID
    public QueryEndpoint<int, Thing> GetThingEndpoint { get; }

    // If your code has dependencies on other services (e.g., HttpClient),
    // you can add them as constructor parameters.
    public MyApi(HttpClient httpClient)
    {
        GetThingEndpoint = new(
            // TODO: Put your query function here.
        );
    }
}
```

2. In `Program.cs`, add the following line (this might vary depending on the template you used):

```cs
builder.Services.AddScoped<MyApi>();
```

3. To use the service, inject it in a component with the `[Inject]` attribute:
```cshtml
@inject MyApi Api
```

</details>

### Creating Queries without Endpoints

If you just need to run a query in a single component and don't want to create an `Endpoint`, another option is to create a `Query` object directly.

```cs
var query = new Query<string, int>((id, cancellationToken) => ...);
```

> :warning: Unlike with Endpoints, you generally shouldn't share a single instance of `Query` across multiple components.

### Using Query Endpoints with `<UseEndpoint/>`

Once you've defined a query endpoint, the best way to use it (in most cases) is with the `<UseEndpoint />` Blazor component. This will handle re-rending the component automatically when the data changes.

If you provide the `Arg` parameter, this will also automatically request new data when the argument changes.

> :information_source: With `<UseParameterlessEndpoint/>`, use the  `AutoFetch` parameter instead of passing an `Arg`.

```cshtml
// This assumes you have created a class called MyApi containing your endpoints,
// and registered it as a singleton or scoped service for dependency injection.
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

For a full working example, view the [sample project](./samples/HackerNewsClient/Pages/PostDetails.razor).

### Using Query objects directly

In cases where the `<UseEndpoint/>` component doesn't provide enough control, you can also use Query objects directly in your code.
This is also useful when using endpoints or queries inside DI services.

`Phetch.Blazor` includes the `<ObserveQuery/>` component for this purpose, so that components can automatically be re-rendered when the query state changes.
This automatically un-subscribes from query events when the component is unmounted (in `Dispose()`), so you don't have to worry about memory leaks.

> :information_source: Alternatively, you can manually subscribe and un-subscribe to a query using the `StateChanged` event.

```cshtml
@inject MyApi Api

@{ query.SetArg(ThingId) }
<ObserveQuery Query="query" OnChanged="StateHasChanged">

@if (query.HasData)
{
    // etc...
}

@code {
    private Query<int, Thing> query = null!;
    [Parameter] public int ThingId { get; set; }

    protected override void OnInitialized()
    {
        query = Api.GetThing.Use();
    }
}
```

### Multiple Parameters

You will often need to define queries or mutations that accept multiple parameters (e.g., a search term and a page number). To do this, you can combine all the parameters into a [tuple](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples), like so:

```cs
var queryEndpoint = new QueryEndpoint<(string searchTerm, int page), List<string>>(
    (args, ct) => GetThingsAsync(args.searchTerm, args.page, ct)
)
```

For cases with lots of parameters, it is recommended to combine them into a [record](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) instead. This will allow you to define default values and other functionality.

### Mutations and Parameterless Queries

Sometimes you will need to use query functions that either have no parameters, or no return value.
In the case of queries without a return value, these are called **Mutations**.
There is also a corresponding class called `MutationEndpoint` (as opposed to the normal `Endpoint`), and a `<UseMutationEndpoint/>` component (as opposed to `<UseEndpoint/>`), which are all designed to work with mutations.

> :information_source: Unlike some other libraries (e.g., React Query and RTK Query), mutations in Phetch behave exactly the same as queries (except for having no return value).

Equivalently, you can use the `ParameterlessEndpoint` class for query functions with no parameters.

### Invoking queries manually

When you use `<UseEndpoint/>` or `<UseMutationEndpoint/>` endpoint and provide an `Arg`, the query will be fetched automatically, using data from the cache if available (see [documentation](#using-query-endpoints-with-useendpoint)).

However, you will sometimes need to control exactly when a query is run. A common use case for this is making requests that modify data on the server (e.g., PUT/POST requests).

The `Query` class contains four different methods for manually invoking queries, depending on your needs:

1. **`SetArg`**: This updates the query argument, and automatically re-fetches the query if the argument has changed. If the same argument is passed multiple times in a row, the query will not be re-fetched.
1. **`Refetch`**: This re-fetches the query using the last query argument passed via `SetArg`.
1. **`Trigger`**: This always runs the query using the passed argument, regardless of whether cached data is available. Importantly, this will **not** share cached data with other components. This is the recommended way to run mutations in most cases.
1. **`Invoke`**: This simply calls the original query function, completely ignoring all Phetch functionality (caching and state management).

### Invalidation and Pessimistic Updates

Often, it will be useful to invalidate or update the data from other queries when a query or mutation completes.

To invalidate query data, you can use the `Invalidate()` methods on an `Endpoint`.
This will cause the affected queries to be automatically re-fetched if they are currently being used.
If they aren't being used, the cached data will be marked as invalidated, and then it will automatically re-fetch if it ever gets used.

Instead of invalidating data, you can also update the cached data directly using `Endpoint.UpdateQueryData()`.

```cs
public class ExampleApi
{
    // An endpoint to retrieve a thing based on its ID
    public Endpoint<int, Thing> GetThingEndpoint { get; }

    // An endpoint with one parameter (the updated thing) and no return
    public MutationEndpoint<Thing> UpdateThingEndpoint { get; }

    public ExampleApi()
    {
        GetThingEndpoint = new(GetThingByIdAsync);

        UpdateThingEndpoint = new(UpdateThingAsync, options: new()
        {
            // Automatically invalidate the cached value for this Thing in GetThingEndpoint,
            // every time this mutation succeeds.
            OnSuccess = eventArgs => GetThingEndpoint.Invalidate(eventArgs.Arg.Id)
        });
    }

    async Task UpdateThingAsync(Thing thing, CancellationToken ct)
    {
        // TODO: Make an HTTP request to update thing
    }

    async Task<Thing> GetThingByIdAsync(int thingId, CancellationToken ct)
    {
        // TODO: Make an HTTP request to get thing
    }

    record Thing(int Id, string Name);
}
```

### Cancellation

Queries that are currently running can be cancelled by calling `query.Cancel()`. This immediately resets the state of the query to whatever it was before the query was started.

This also cancels the `CancellationToken` that was passed to the query function, but this **only has an effect if you used the `CancellationToken` in your query function**.
If you pass the `CancellationToken` to the HTTP client (see the code sample in [Defining Query Endpoints](#defining-query-endpoints-recommended)), the browser will automatically cancel the in-flight request when you call `query.Cancel`.

It is still up to your API to correctly handle the cancellation, so you should not rely on this to cancel requests that modify data on the server.

### Pre-fetching

If you know which data your user is likely to request in the future, you can call `endpoint.Prefetch(arg)` to trigger a request ahead of time and store the result in the cache.
For example, you can use this to automatically fetch the next page of data in a table, which is demonstrated in the [sample project](./samples/PhetchBlazorDemo/Pages/Pagination.razor).

If you already know what the query data will be, you can use `endpoint.UpdateQueryData(arg, data)` to add or update a cache entry without needing to run the query again.