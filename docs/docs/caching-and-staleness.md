## StaleTime

Phetch uses the concept of "staleness" to determine when to re-fetch data.
If a query requests data that is already cached (e.g. because another component requested it) and the cached data is **not** stale, the query will receive the cached result and won't re-fetch the data. 
If the cached data **is** stale, the cached data will be used initially, but new data will be re-fetched in the background automatically.
This is known as the stale-while-revalidate pattern.

By default, queries in Phetch are marked as stale as soon as they return, so subsequent requests will always cause a refetch in the background.
You can customise this behaviour by setting the `StaleTime` option when creating an endpoint, or when calling `endpoint.Use(...)` to override the endpoint setting.
This controls the amount of time between when a query returns, and when the result is marked as stale.

To stop cached data from ever becoming stale, set the `StaleTime` to `TimeSpan.MaxValue`. If you also want cached responses to last indefinitely, see [CacheTime](#cachetime).

## CacheTime

By default, cached results will be removed from the cache if they are not used for more than 5 minutes.
You can customize this using the `CacheTime` option when creating an endpoint.
To make cached values last forever, set this to `TimeSpan.MaxValue`.