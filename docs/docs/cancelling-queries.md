# Cancelling queries

Queries that are currently running can be cancelled by calling `query.Cancel()`. This immediately resets the state of the query to whatever it was before the query was started.

This also cancels the `CancellationToken` that was passed to the query function, but this **only has an effect if you used the `CancellationToken` in your query function**.
If you pass the `CancellationToken` to the HTTP client (see the code sample in [Defining Query Endpoints](./defining-query-endpoints.md)), the browser will automatically cancel the in-flight request when you call `query.Cancel`.

It is still up to your API to correctly handle the cancellation, so you should not rely on this to cancel requests that modify data on the server, without also checking whether the cancellation succeeded.