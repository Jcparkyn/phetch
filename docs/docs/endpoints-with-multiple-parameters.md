# Endpoints with multiple parameters

You will often need to define endpoints that accept multiple parameters (e.g., a search term and a page number). To do this, you can combine all the parameters into a [tuple](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples), like so:

```cs
var endpoint = new Endpoint<(string searchTerm, int page), List<string>>(
    (args, ct) => GetThingsAsync(args.searchTerm, args.page, ct)
)
```

For cases with lots of parameters, it is usually better to combine them into a [record](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) instead. This will allow you to define default values and other functionality.

> :warning: Be careful when using classes or other mutable types as query parameters. Phetch uses the object's `GetHashCode()` and `Equals()` methods to determine whether the query needs to be re-fetched, so mutating a query argument after using it can have unexpected results.