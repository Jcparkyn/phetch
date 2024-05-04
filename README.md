# Phetch&nbsp; [![Nuget](https://img.shields.io/nuget/v/Phetch.Blazor)](https://www.nuget.org/packages/Phetch.Blazor) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![codecov](https://codecov.io/github/Jcparkyn/phetch/graph/badge.svg?token=S3VNHVJPD4)](https://codecov.io/github/Jcparkyn/phetch)

### [Documentation](https://jcparkyn.github.io/phetch/docs/getting-started.html) | [API Reference](https://jcparkyn.github.io/phetch/api/Phetch.Blazor.html) | [Sample app](./samples/HackerNewsClient)

Phetch is a small Blazor library for handling async query state, in the style of [React Query](https://github.com/tannerlinsley/react-query), [SWR](https://github.com/vercel/swr), or [RTK Query](https://redux-toolkit.js.org/rtk-query/overview).

Currently, Phetch is only designed for use with Blazor WebAssembly. However, the core package (Phetch.Core) has no dependencies on Blazor or ASP.NET Core, so in theory it can be used anywhere that supports .NET Standard 2.1.

| :information_source: Status: All core features are finished, but there may be some minor breaking changes before a v1.0.0 release. |
|:-|

## Features
- Automatically handles loading and error states, and updates your components whenever the state changes.
- Automatically caches data returned by queries, and makes it easy to invalidate or update this cached data when needed.
- Supports calling any async method as a query (not restricted just to HTTP requests).
- Supports pagination, prefetching, request de-duplication, retries, dependent queries, CancellationTokens, and more.
- 100% strongly typed, with nullability annotations.
- Lightweight and easy to combine with other state management methods.
- No Javascript needed!

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
        <p>Something went wrong!</p>
    } else if (query.IsLoading) {
        <p>Loading...</p>
    } else if (query.HasData) {
        <p>The number is @(query.Data ? "even" : "odd")</p>
    }
</UseEndpoint>
```

<details>
<summary>Some notes on the example above</summary>

- Inside the `<UseEndpoint>` component, you can use `query` to access the current state of the query. Changing the `Context` parameter will rename this object.
- By changing the `Arg` parameter, the query will automatically be re-fetched when needed.
- Normally, you would share endpoints around your application using dependency injection (see [Defining Query Endpoints](#defining-query-endpoints-recommended)).
- If you need to access the query state inside the `@code` block of a component, you can replace `<UseEndpoint/>` with the pattern described in [Using Query Objects Directly](#using-query-objects-directly).

</details>
</br>

[Click here to go to the full documentation](https://jcparkyn.github.io/phetch/docs/getting-started.html)

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
  - However, for managing query state, you will need much more code to achieve the same things that Phetch can do in just a couple of lines. This becomes particularly important if you need to cache data or share queries across components.
- **[Fusion](https://github.com/servicetitan/Stl.Fusion)**: This is a much larger library, focused on real-time updates. Fusion is great if your app has lots of real-time functionality, but for most applications it will probably be overkill compared to Phetch.
