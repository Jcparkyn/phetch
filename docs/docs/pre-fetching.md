# Pre-fetching

If you know which data your user is likely to request in the future, you can call `endpoint.Prefetch(arg)` to trigger a request ahead of time and store the result in the cache.
For example, you can use this to automatically fetch the next page of data in a table, which is demonstrated in the [sample project](https://github.com/jcparkyn/phetch/blob/main/samples/PhetchBlazorDemo/Pages/Pagination.razor).

If you already know what the query data will be, you can use `endpoint.UpdateQueryData(arg, data)` to add or update a cache entry without needing to run the query again.