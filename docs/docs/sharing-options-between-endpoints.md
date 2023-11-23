# Sharing options between endpoints

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
