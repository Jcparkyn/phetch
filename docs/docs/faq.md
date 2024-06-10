# Frequently Asked Questions

## Troubleshooting

### Q: Why is my query not re-fetching when I change the arguments?

Most likely, this is because your query parameter is a mutable class. Ideally, **all your query parameters should be primitives, tuples, or immutable records**. If you pass a class instance as a query parameter, then modify it and pass it again, Phetch has no way to know whether the argument changed. See [endpoints with multiple parameters](./endpoints-with-multiple-parameters.md) for more info.

### Q: Why is my component not updating when the query succeeds?

Unless you're using `<UseEndpoint/>`, this probably means you forgot to subscribe your component to the query. You can either do this by:
- Adding `<ObserveQuery Target="query" OnChanged="StateHasChanged"/>` to the top of your component.
- Wrapping the part of your component that uses query data inside `<ObserveQuery Target="query">...</ObserveQuery>`.
- Subscribing and unsubscribing from the `StateChanged` event.

### Q: Why is my query being re-fetched even though it already has data in the cache?
This is because the default `StaleTime` for an endpoint is zero seconds. See [caching and staleness](./caching-and-staleness.md) for more info.

## Defining queries

### Q: How do I define a mutation?

If you're coming from React Query, you may be used to "queries" and "mutations" being different things.
In Phetch, these have been combined, so that everything is just a query.
To get the same behavior as a mutation in React Query or RTK Query, use the `query.Trigger()` method.
See [invoking queries manually](./invoking-queries-manually.md) for more info.

### Q: How do I define a query with no parameters?

See [endpoints without parameters and/or return values](./endpoints-without-parameters.md).

### Q: How do I define a query with no return value?

See [endpoints without parameters and/or return values](./endpoints-without-parameters.md).

## Best Practices

### Q: Should I use `SetArg` or `SetArgAsync`?

Most of the time you should use the **non-async** variants of query methods (`SetArg`, `Refetch`, `Trigger`). Only use the `Async` variants if you need to `await` the query and/or access the result, but keep in mind that these methods will **re-throw the exception** if the query fails.

### Q: What order should I check for error/loading/success?

This is mostly up to you, but _make sure to test how your component handles query errors_. That said, a general pattern I recommend is:

```cs
@if(query.IsLoading) {
  // render loading indicator or skeleton
} else if (query.IsError) {
  // render error message
} else if (query.HasData) { // remove this check if data can be null
  if (query.IsFetching) {
    // optional: render background fetch indicator
  }
  // render data
}
```

If you're using `LastData` (e.g. for pagination), you may want to use a different pattern.


### Q: What is the `CancellationToken` in the query function for?

This is optional, for if you want to be able to cancel your queries while they are running. See [Cancelling Queries](./cancelling-queries.md) for more info.

### Q: How should I unit test components that use an endpoint?

My recommendation is that you should _not_ try to mock out your `Api` classes or the endpoints you inject into components.
The way the endpoint behaves is an important part of your component's behaviour, and because of the complexities of data fetching, it's difficult to mock accurately.
Instead, **mock the dependencies being passed into your `Api` class**.
For example, you can either:
- Mock the raw HTTP responses from your API using something like [mockhttp](https://github.com/richardszalay/mockhttp), and pass the mocked `HttpClient` into your `Api` constructor.
- Wrap your HTTP calls into a separate class/interface (e.g. MyApiClient), then inject _that_ into your `Api` class. Then you can mock that interface using your mocking library of choice.
