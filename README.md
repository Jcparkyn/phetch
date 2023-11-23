# Phetch

Phetch is a small Blazor library for handling async query state, in the style of [React Query](https://github.com/tannerlinsley/react-query), [SWR](https://github.com/vercel/swr), or [RTK Query](https://redux-toolkit.js.org/rtk-query/overview).

Currently, Phetch is only designed for use with Blazor WebAssembly. However, the core package (Phetch.Core) has no dependencies on Blazor or ASP.NET Core, so in theory it can be used anywhere that supports .NET Standard 2.1.

| :information_source: Status: All core features are finished, but there may be some minor breaking changes before a v1.0.0 release. |
|:-|

## Features
- Automatically handles loading and error states, and updates your components whenever the state changes
- Automatically caches data returned by queries, and makes it easy to invalidate or update this cached data when needed.
- Supports calling any async method as a query (not restricted just to HTTP requests)
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

### Defining Query Endpoints

Start by defining a query using the `Endpoint` class.
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
    public Endpoint<int, Thing> GetThingEndpoint { get; }

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

### Using Query Endpoints with `<UseEndpoint/>`

Once you've defined a query endpoint, the best way to use it (in most cases) is with the `<UseEndpoint/>` Blazor component. This will handle re-rending the component automatically when the data changes.

If you provide the `Arg` parameter (the value to pass to the endpoint), this will also automatically fetch the data, and request new data when the argument changes. Without an `Arg` parameter (or with `AutoFetch="false"`), the data will not be fetched automatically.

> :information_source: With `<UseParameterlessEndpoint/>`, you don't need to supply an `Arg`.
If you don't want the query to be fetched automatically, you can set `AutoFetch="false"`.

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
<ObserveQuery Target="query" OnChanged="StateHasChanged">
```

### Multiple Parameters

You will often need to define endpoints that accept multiple parameters (e.g., a search term and a page number). To do this, you can combine all the parameters into a [tuple](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples), like so:

```cs
var endpoint = new Endpoint<(string searchTerm, int page), List<string>>(
    (args, ct) => GetThingsAsync(args.searchTerm, args.page, ct)
)
```

For cases with lots of parameters, it is usually better to combine them into a [record](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) instead. This will allow you to define default values and other functionality.

> :warning: Be careful when using classes or other mutable types as query parameters. Phetch uses the object's `GetHashCode()` and `Equals()` methods to determine whether the query needs to be re-fetched, so mutating a query argument after using it can have unexpected results.

### Queries without parameters or return values

Sometimes you will need to use query functions that either have no parameters, or no return value.

For endpoints without parameters, you can use the `ParameterlessEndpoint` class, which is a subclass of `Endpoint` that accepts no parameters.
In your components, use the `<UseParameterlessEndpoint/>` to call these endpoints.

Similarly, use the `ResultlessEndpoint` class for endpoints that return no value. This can be used with the normal `<UseEndpoint/>` component.

> :information_source: If you use these classes, you may notice that some methods and types use the `Unit` type. This is a type used by Phetch to represent the absence of a value, which allows all classes to derive from the base `Query<TArg, TResult>` and `Endpoint<TArg, TResult>` types.

### Invoking queries manually

When you use `<UseEndpoint/>` and provide an `Arg`, the query will be fetched automatically, using data from the cache if available (see [Using query endpoints with UseEndpoint](#using-query-endpoints-with-useendpoint)).

However, you will sometimes need to control exactly when a query is run. A common use case for this is making requests that modify data on the server (e.g., PUT/POST requests).

The `Query` class contains four different methods for manually invoking queries, depending on your needs:

1. **`SetArg`**: This updates the query argument, and automatically re-fetches the query if the argument has changed. If the same argument is passed multiple times in a row, the query will not be re-fetched. This is what `<UseEndpoint/>` calls internally.
1. **`Refetch`**: This re-fetches the query in the background, using the last query argument passed via `SetArg`.
1. **`Trigger`**: This always runs the query using the passed argument, regardless of whether cached data is available. Importantly, this will **not** share cached data with other components. This is the recommended way to call endpoints with side-effects (e.g. PUT, POST, or DELETE endpoints) in most cases, and works a lot like mutations in React Query.
1. **`Invoke`**: This simply calls the original query function, completely ignoring all Phetch functionality (caching and state management).

> :information_source: If you are coming from React Query, you may be used to "queries" and "mutations" being different things.
In Phetch, these have been combined, so that everything is just a query.
To get the same behavior as a mutation in React Query or RTK Query, use the `query.Trigger()` method.

### StaleTime

Phetch uses the concept of "staleness" to determine when to re-fetch data.
If a query requests data that is already cached (e.g. because another component requested it) and the cached data is **not** stale, the query will receive the cached result and won't re-fetch the data. 
If the cached data **is** stale, the cached data will be used initially, but new data will be re-fetched in the background automatically.
This is known as the stale-while-revalidate pattern.

By default, queries in Phetch are marked as stale as soon as they return, so subsequent requests will always cause a refetch in the background.
You can customise this behaviour by setting the `StaleTime` option when creating an endpoint, or when calling `endpoint.Use(...)` to override the endpoint setting.
This controls the amount of time between when a query returns, and when the result is marked as stale.

To stop cached data from ever becoming stale, set the `StaleTime` to `TimeSpan.MaxValue`. If you also want cached responses to last indefinitely, see [CacheTime](#cachetime).

### CacheTime

By default, cached results will be removed from the cache if they are not used for more than 5 minutes.
You can customize this using the `CacheTime` option when creating an endpoint.
To make cached values last forever, set this to `TimeSpan.MaxValue`.

### Invalidation and Pessimistic Updates

Often, it will be useful to invalidate or update the data from other queries when a query completes.

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
    public ResultlessEndpoint<Thing> UpdateThingEndpoint { get; }

    public ExampleApi()
    {
        GetThingEndpoint = new(GetThingByIdAsync);

        UpdateThingEndpoint = new(UpdateThingAsync, options: new()
        {
            // Automatically invalidate the cached value for this Thing in GetThingEndpoint,
            // every time this query succeeds.
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

It is still up to your API to correctly handle the cancellation, so you should not rely on this to cancel requests that modify data on the server, without also checking whether the cancellation succeeded.

### Pre-fetching

If you know which data your user is likely to request in the future, you can call `endpoint.Prefetch(arg)` to trigger a request ahead of time and store the result in the cache.
For example, you can use this to automatically fetch the next page of data in a table, which is demonstrated in the [sample project](./samples/PhetchBlazorDemo/Pages/Pagination.razor).

If you already know what the query data will be, you can use `endpoint.UpdateQueryData(arg, data)` to add or update a cache entry without needing to run the query again.

### Retries

To improve the resilience of your application, you can configure Phetch to automatically retry failed queries, using the `RetryHandler` option.
If you just want to retry a fixed number of times, you can use `RetryHandler.Simple` like so:

```cs
var isEvenEndpoint = new Endpoint<int, bool>(
    GetIsEvenAsync, // Put you query function here
    options: new()
    {
        // Retry a maximum of two times on any exception (except cancellation)
        RetryHandler = RetryHandler.Simple(2),
    }
);
```

If you need to override the default retry behaviour of an endpoint in a single component, you can just pass a new `RetryHandler` to the options of `endpoint.Use`.
To remove an existing retry handler entirely, use `RetryHandler.None`.

For more advanced use-cases, you can create your own class that implements `IRetryHandler`.
See [the implementation of SimpleRetryHandler](./src/Phetch.Core/RetryHandler.cs) for an example of how to do this. Alternatively, you can integrate Phetch with [Polly](https://github.com/App-vNext/Polly) as shown below:

<details>
<summary>Integrating Phetch with Polly for advanced retries</summary>

Start by adding the following adapter class anywhere in your project:

```cs
using Phetch.Core;
using Polly;

public sealed record PollyRetryHandler(IAsyncPolicy Policy) : IRetryHandler
{
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct) =>
        Policy.ExecuteAsync(queryFn, ct);
}
```

Then, you can pass an instance of `PollyRetryHandler` to you endpoint or query:
```cs
var policy = Policy
    .Handle<HttpRequestException>()
    .RetryAsync(retryCount);

var endpointOptions = new EndpointOptions
{
    RetryHandler = new PollyRetryHandler(policy)
};
```

</details>

### Sharing options between endpoints

Phetch does not directly provide a way to set the "default" options across all endpoints.
Instead, you can create an instance of `EndpointOptions` with the default settings you want, and then manually use or extend this in each of your endpoints.

```cs
var defaultEndpointOptions = new EndpointOptions
{
    CacheTime = TimeSpan.FromMinutes(2),
    OnFailure = event => Console.WriteLine(event.Exception.Message),
    RetryHandler = RetryHandler.Simple(2),
};
```

You can then pass this directly to an `Endpoint` constructor, or pass it to the constructor of a new `EndpointOptions` to make a modified copy.

```cs
var isEvenEndpoint = new Endpoint<int, bool>(
    GetIsEvenAsync, // Put your query function here
    options: new(defaultEndpointOptions)
    {
        CacheTime = TimeSpan.FromMinutes(10),
    }
);
```

> :information_source: The difference between `EndpointOptions` and `EndpointOptions<TArg, TResult>` is intentional. Endpoint constructors can accept either, but you will need to use `EndpointOptions<TArg, TResult>` if you want to access the `Arg` and `Result` properties in the `OnSuccess` and `OnFailure` callbacks.

Endpoint options are immutable, so it is safe (and recommended) to make your "default" options instance static.
