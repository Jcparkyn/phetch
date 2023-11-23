# Invalidation and Pessimistic Updates

Often, it will be useful to invalidate or update the data from other queries when a query completes.

To invalidate query data, you can use the `Invalidate()` methods on an `Endpoint`.
This will cause the affected queries to be automatically re-fetched if they are currently being used.
If they aren't being used, the cached data will be marked as invalidated, and then it will automatically re-fetch if it ever gets used.

Instead of invalidating data, you can also update the cached data directly using `Endpoint.UpdateQueryData()`.

```cs
public class ExampleApi
{
    // An endpoint to retrieve a thing based on its ID
    public Endpoint<int, Thing> GetThingEndpoint { get; }

    // An endpoint with one parameter (the updated thing) and no return
    public ResultlessEndpoint<Thing> UpdateThingEndpoint { get; }

    public ExampleApi()
    {
        GetThingEndpoint = new(GetThingByIdAsync);

        UpdateThingEndpoint = new(UpdateThingAsync, options: new()
        {
            // Automatically invalidate the cached value for this Thing in GetThingEndpoint,
            // every time this query succeeds.
            OnSuccess = eventArgs => GetThingEndpoint.Invalidate(eventArgs.Arg.Id)
        });
    }

    async Task UpdateThingAsync(Thing thing, CancellationToken ct)
    {
        // TODO: Make an HTTP request to update thing
    }

    async Task<Thing> GetThingByIdAsync(int thingId, CancellationToken ct)
    {
        // TODO: Make an HTTP request to get thing
    }

    record Thing(int Id, string Name);
}
```