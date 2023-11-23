# Retries

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
See [the implementation of SimpleRetryHandler](https://github.com/jcparkyn/phetch/blob/main/src/Phetch.Core/RetryHandler.cs) for an example of how to do this. Alternatively, you can integrate Phetch with [Polly](https://github.com/App-vNext/Polly) as shown below:

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
