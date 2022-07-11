# Phetch

Phetch is a small Blazor library for handling async query state, in the style of [React Query](https://github.com/tannerlinsley/react-query) or [SWR](https://github.com/vercel/swr).

Currently, Phetch is only designed for use with Blazor WebAssembly. However, there are no dependencies on Blazor or ASP.NET Core, so in theory it can be used anywhere that supports .NET Standard 2.1.

| :warning: Note: Phetch is in early development and likely to change. |
|:---------------------------------------------------------------------|

## Features
- Automatically handles loading and error states, and updates your components whenever the state changes
- Use any async method as a query or mutation (not restricted just to HTTP requests)
- Built-in support for CancellationTokens
- Built-in debouncing, to (optionally) limit the rate of queries being sent
- Supports mutations, dependent queries, and pagination
- 100% strongly typed, with nullability annotations
- Super lightweight and easy to mix-and-match with other state management methods
- No Javascript whatsoever!

## But why?

You're probably familiar with the "normal" way of performing async actions in client-side Blazor.
You start a request in `OnInitializedAsync`, and then update a field when it returns.

But then you need to add error  handling, so you add another variable to keep track of whether there was an error.
Then once your component gets more complicated, you need to add another variable to explicitly track whether the query is loading.
Then you realise that your query depends on values from your parameters, so you need to add custom logic in `OnParametersSetAsync` to manually re-start the query, but only when the parameters change.
Then you need to be able to cancel queries after they've started, so you add a `CancellationTokenSource` and all the corresponding logic.
If you've been paying attention, you might notice that your queries sometimes return in the wrong order because your server doesn't always take the same amount of time.
And then you get to the next component and do it all over again.

Phetch aims to solve all of these problems.

## Show me some code!

[Click here to view the source code for the sample project, with more detailed examples.](https://github.com/Jcparkyn/Phetch/tree/main/samples/PhetchBlazorDemo)

Below is the code for a super-basic component that runs a query when the component is first loaded.
Phetch can do a whole lot more than that though, so make sure to check out the samples project and full documentation!

```csharp
@inject HttpClient Http

@if (forecastsQuery.IsError)
{
    <p><em>Error!</em></p>
}
else if (forecastsQuery.IsLoading)
{
    <p><em>Loading...</em></p>
}
else
{
    // Render data from forecastsQuery.Data
}

@code {
    private Query<WeatherForecast[]> forecastsQuery = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        forecastsQuery = new(
            _ => Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json")!,
            onStateChanged: StateHasChanged
        );
    }
}

```

Phetch will also come with some useful extension methods to do things like this:

```cshtml
<p>
    This number is: @isEvenQuery.Match(
        fetching: () => @<text>...</text>,
        error: ex => @<em>Something went wrong!</em>,
        success: isEven => @<b>@(isEven ? "even" : "odd")</b>
    )
</p>
```

## Installing

Note: Because many features have not been finalized, I won't yet be updating the NuGet version on a regular basis.
If you want to try out Phetch in the meantime, I would recommend downloading the source code instead.

You can install Phetch via the .NET CLI with the following command:

```sh
dotnet add package Phetch.Blazor
```

If you're using Visual Studio, you can also install via the built in NuGet package manager.


## Usage

There are a few different ways to define and use queries, depending on your use case.

### Query Endpoints (Recommended)

In most cases, the best way to define queries is to use the `QueryEndpoint` class.

#### Defining Query Endpoints
```cs
// This defines an endpoint that takes an int and returns a bool.
var isEvenEndpoint = new QueryEndpoint<int, bool>(
    // Replace this part with your own async function:
    async val =>
    {
        var response = await httpClient.GetFromJsonAsync<dynamic>($"https://api.isevenapi.xyz/api/iseven/{val}");
        return response.IsEven;
    }
);
```

You can then share this instance of QueryEndpoint across your whole application and use it wherever you need it.
In most cases, the best way to do this is with Blazor's built-in [dependency injection](https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection).
You can view the [sample project](./samples/HackerNewsClient/Shared/HackerNewsApi.cs) for a full example of how to do this, or follow the steps below:

<details>
<summary>Setting up dependency injection (DI)</summary>

1. Create a class containing an instance of `QueryEndpoint`. You can have as many or few endpoints in a class as you want.
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

#### Using Query Endpoints

Once you've defined a query endpoint, the best way to use it (in most cases) is with the `<UseQueryEndpoint />` Blazor component. This will automatically request new data when the parameters change, and will handle re-rending the component when the data changes.

```cshtml
// This assumes you have created a class called MyApi containing your endpoints,
// and registered it as a singleton or scoped service for dependency injection.
@inject MyApi Api

<UseQueryEndpoint Endpoint="@Api.GetThing" Param="ThingId" Context="query">
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
</UseQueryEndpoint>

@code {
    [Parameter]
    public int ThingId { get; set; }
}
```

For a full working example, view the [sample project](./samples/HackerNewsClient/Pages/PostDetails.razor).

### Multiple Parameters

You will often need to define queries or mutations that accept multiple parameters (e.g., a search term and a page number). To do this, you can combine all the parameters into a [tuple](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples), like so:

```cs
var queryEndpoint = new QueryEndpoint<(string searchTerm, int page), List<string>>(
    args => GetThingsAsync(args.searchTerm, args.page)
)
```

For cases with lots of parameters, it is recommended to combine them into a [record](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) instead. This will allow you to define default values and other functionality.

## Mutations

So far, the documentation has talked about queries, which are useful for _retrieving_ (querying) data.
However, you will often need to _update_ (mutate) data on a server.
While you can technically do this using queries, the default behaviour won't often be what you want.
This is where mutations come in.
The main differences between queries and mutations are:

- Mutations are _not_ run automatically as soon as they are requested (e.g., via `<UseMutationEndpoint/>`). Instead, mutations have a `Trigger` method so that they can be called when needed.
- Mutations do _not_ use a cache.
- Mutations are generally used for methods with side effects (e.g. an HTTP POST endpoint).

Aside from these points, the usage of mutations is generally very similar to that of queries, but with `MutationEndpoint` and `<UseMutationEndpoint/>` instead of `QueryEndpoint` and `<UseQueryEndpoint/>`.

Often, it will be useful to invalidate or update the data from other queries when a mutation completes, like so:

```cs
public class ExampleApi
{
    // An endpoint to retrieve a thing based on its ID
    public QueryEndpoint<int, Thing> GetThingEndpoint { get; }

    // An endpoint with one parameter (the updated thing) and no return
    public MutationEndpoint<Thing> UpdateThingEndpoint { get; }

    public ExampleApi()
    {
        GetThingEndpoint = new(GetThingByIdAsync);

        UpdateThingEndpoint = new(async thing => 
        {
            await UpdateThingAsync(thing);
            GetThingEndpoint.Invalidate(thing.Id);
        });
    }

    async Task UpdateThingAsync(Thing thing)
    {
        // TODO: Make an HTTP request to update thing
    }

    async Task<Thing> GetThingByIdAsync(int thingId)
    {
        // TODO: Make an HTTP request to get thing
    }
}
```