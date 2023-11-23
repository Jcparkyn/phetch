# Defining Query Endpoints

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
You can view the [sample project](https://github.com/jcparkyn/phetch/blob/main/samples/HackerNewsClient/Shared/HackerNewsApi.cs) for a full example of how to do this, or follow the steps below:

## Setting up dependency injection (DI)

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

3. To use the service, inject it in a component using `@inject` or the `[Inject]` attribute:
```cshtml
@inject MyApi Api
```
