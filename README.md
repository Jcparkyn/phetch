# Fetcher

Fetcher is a small Blazor library for handling async query state, in the style of [React Query](https://github.com/tannerlinsley/react-query) or [SWR](https://github.com/vercel/swr).

Currently, Fetcher is only designed for use with Blazor WebAssembly. However, there are no dependencies on Blazor or ASP.NET Core, so in theory it can be used anywhere that supports .NET Standard 2.1.

| :warning: Note: Fetcher is in early development and likely to change. |
|:----------------------------------------------------------------------|

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

Fetcher aims to solve all of these problems.

## Show me some code!

[Click here to view the source code for the sample project, with more detailed examples.](https://github.com/Jcparkyn/Fetcher/tree/main/samples/FetcherBlazorDemo)

Below is the code for a super-basic component that runs a query when the component is first loaded.
Fetcher can do a whole lot more than that though, so make sure to check out the samples project and full documentation!

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

Fetcher will also come with some useful extension methods to do things like this:

```html
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
If you want to try out Fetcher in the meantime, I would recommend downloading the source code instead.

You can install Fetcher via the .NET CLI with the following command:

```sh
dotnet add package Fetcher
```

If you're using Visual Studio, you can also install via the built in NuGet package manager.