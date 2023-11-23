# Invoking queries manually

When you use `<UseEndpoint/>` and provide an `Arg`, the query will be fetched automatically, using data from the cache if available (see [Using query endpoints with UseEndpoint](./useendpoint.md)).

However, you will sometimes need to control exactly when a query is run. A common use case for this is making requests that modify data on the server (e.g., PUT/POST requests).

The `Query` class contains four different methods for manually invoking queries, depending on your needs:

1. **`SetArg`**: This updates the query argument, and automatically re-fetches the query if the argument has changed. If the same argument is passed multiple times in a row, the query will not be re-fetched. This is what `<UseEndpoint/>` calls internally.
1. **`Refetch`**: This re-fetches the query in the background, using the last query argument passed via `SetArg`.
1. **`Trigger`**: This always runs the query using the passed argument, regardless of whether cached data is available. Importantly, this will **not** share cached data with other components. This is the recommended way to call endpoints with side-effects (e.g. PUT, POST, or DELETE endpoints) in most cases, and works a lot like mutations in React Query.
1. **`Invoke`**: This simply calls the original query function, completely ignoring all Phetch functionality (caching and state management).

> :information_source: If you are coming from React Query, you may be used to "queries" and "mutations" being different things.
In Phetch, these have been combined, so that everything is just a query.
To get the same behavior as a mutation in React Query or RTK Query, use the `query.Trigger()` method.
