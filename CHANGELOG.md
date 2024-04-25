

# Unreleased

## Added
- The non-generic `QueryOptions` class can now be implicitly cast to `QueryOptions<TArg, TResult>`.

## Changed
- **BREAKING CHANGE**: If a query succeeds and then fails on a refetch, `query.Data` now returns `null`/`default`, instead of the old data. Also, `query.LastData` now returns the data from the last successful request (even if the arg was different) instead of `null`/`default`.
- `QueryOptions.OnSuccess` is now called when a cached result is returned (unless it's stale).

## Fixed
-  If a query fails during a refetch, `query.LastData` now returns the last successful data for this arg, rather than falling back to a previous arg.

# [v0.6.0] - 1 Dec 2023

## Added
- New DocFX site for documentation
- Updated docs for CacheTime and StaleTime
- If you're targeting .NET 8, you'll now have proper hot reload support in components containing `<UseEndpoint/>` or `<ObserveQuery/>`. This isn't specific to this version of Phetch, but worth mentioning anyway.
- `UseEndpointInfinite.LoadNextPageAsync` now returns the result of the next page query, or throws if the next page can't be loaded yet (e.g., the last page failed).

## Changed
- **BREAKING CHANGE**: `<ObserveQuery/>` no longer detaches queries automatically when disposed. To keep the same behaviour as before, set `DetachWhenDisposed="true"`, or explicitly call `query.Dispose()` in the `Dispose` method of your component.
- **BREAKING CHANGE**: Changed the function signature for the `GetNextPageArg` option of `<UseEndpointInfinite/>`.
- Removed the `<UseMutationEndpoint/>` component that was previously marked as `[Obsolete]`.

## Fixed
- Fixed events not being properly unsubscribed when `<ObserveQuery/>` was disposed.
- Fixed failed queries in cache not automatically re-fetching if they have previously succeeded.

## Development
- Lots more tests.
- Upgraded sample projects to .NET 8

[Changes][v0.6.0]

<a name="v0.5.1"></a>
# [v0.5.1](https://github.com/Jcparkyn/phetch/releases/tag/v0.5.1) - 14 Jul 2023

## Changed
- Queries now trigger a `StateChanged` event when they begin fetching. This makes the behavior a bit more predictable when using endpoints across multiple components.
- Improved docs for `<ObserveQuery/>`.

## Added
- New `onSuccess` and `onFailure` callback parameters for `query.Trigger()` methods, which get called when the query succeeds or fails, respectively. Thanks to [@Paxol](https://github.com/Paxol) for suggesting this.

[Changes][v0.5.1]


<a name="v0.5.0"></a>
# [v0.5.0](https://github.com/Jcparkyn/phetch/releases/tag/v0.5.0) - 19 Jun 2023

## Changed
- **BREAKING CHANGE**: Removed public constructors for `Query` that were previously deprecated.
- **BREAKING CHANGE**: Renamed `MutationEndpoint<T>` to `ResultlessEndpoint<T>` for better consistency. The old `MutationEndpoint<T>` has been marked as obsolete, and left in temporarily to make migration easier. Also removed `Mutation<T>` and  `<UseMutationEndpoint/>`, which can safely be replaced with `Query<T, Unit>` and `<UseEndpoint/>`.
- Moved some of the methods on `Query<T>` to extension methods, so that they would also work on the equivalent `Query<Unit, T>` class.
- Lots of improvements to documentation and samples.
- More tests.

## Fixed
- Fixed some issues that arise when `Skip` and `Arg` parameters of `UseEndpoint` are changed simultaneously.

[Changes][v0.5.0]


<a name="v0.4.0"></a>
# [v0.4.0](https://github.com/Jcparkyn/phetch/releases/tag/v0.4.0) - 06 Mar 2023

## Added
- `Endpoint.TryGetCachedResult` method, to retrieve a cached result synchronously for the given query argument.
- New `IQuery` and `IQuery<TArg, TResult>` interfaces, to help with testing or writing methods that operate on queries.
- Lots more tests (currently 96% line coverage in `Phetch.Core`).
- Lots of improvements to the sample projects (in [samples](https://github.com/Jcparkyn/Phetch/tree/v0.4.0/samples)).

## Changed
- Public constructors for `Query` are now deprecated, because they serve very little value since adding Endpoints. Instead, just create an endpoint and call `.Use()`. These constructors will be removed in a future release.

## Fixed
- Exceptions in `OnSuccess`/`OnFailure` callbacks are now caught, instead of being incorrectly treated as a query failure.

[Changes][v0.4.0]


<a name="v0.3.1"></a>
# [v0.3.1](https://github.com/Jcparkyn/phetch/releases/tag/v0.3.1) - 21 Nov 2022

## Added
- Lots more tests
- `Query` and `Endpoint` classes now have a public `Options` property to read the options that were passed to them.
- `Query` now has a `CurrentQuery` property to get the `FixedQuery` that it is currently observing.
- New `KeySelector` option to customize the value used for caching query arguments.
- Support for infinite `StaleTime` by passing a negative value

## Changed
- `RefetchAsync` and similar methods now return the result data from the query.

## Fixed
- **BREAKING CHANGE**: `Query.Refetch` now throws an exception if it is un-initialized, instead of failing silently.
- `null` can now be used as a query argument.
- Fixed bug where large `StaleTime` values (e.g., `TimeSpan.MaxValue) would cause an overflow.


[Changes][v0.3.1]


<a name="v0.3.0"></a>
# [v0.3.0](https://github.com/Jcparkyn/phetch/releases/tag/v0.3.0) - 13 Nov 2022

## Added
- New parameter for `UpdateQueryData`  to optionally insert a new cache entry if one doesn't already exist for the given argument.
- Read-only `Arg` property on `Query` class, to access the last argument passed to `SetArg`.
- Option to pass child content directly to `<ObserveQuery/>`, so that only the child content is re-rendered when the query updates. When using this method, there is no need to pass the `OnChanged="StateHasChanged"` parameter.
- New `RetryHandler` option for endpoints and queries, to control whether and how the query retries upon failure.
- New non-generic `EndpointOptions` and `QueryOptions` classes, which can be used to share "default" settings between endpoints and queries.
- New experimental `<UseEndpointInfinite/>` component for creating "infinite scroll" features. To use this, add `@using Phetch.Blazor.Experimental` to your `_Imports.razor`.

## Changed
- **BREAKING CHANGE**: Removed the return value from `UpdateQueryData`
- **BREAKING CHANGE**: Removed the `arg` parameter from `InvalidateWhere`. Instead, use the `Arg` property of the query.
- Various improvements to documentation and sample projects.

[Changes][v0.3.0]


<a name="v0.2.0"></a>
# [v0.2.0](https://github.com/Jcparkyn/phetch/releases/tag/v0.2.0) - 18 Aug 2022

## Added
- New `GetCachedQuery` method on `Endpoint` to look up a cache entry.
  - The returned cache entry can also be invalidated with `Invalidate()`, or have its data updated with `UpdateQueryData(...)`.
- New overload for `UpdateQueryData` which takes a function of the old query data.
- New `DefaultStaleTime` option for `Endpoint`, which sets the stale time when no value is passed to `Use`.
- New synchronous `Prefetch()` method on `Endpoint`, which doesn't return a Task.

## Changed
- **BREAKING CHANGE**: Rename `QuerySuccessContext` and `QueryFailureContext` to `QuerySuccessEventArgs` and `QueryFailureEventArgs`.
- Various improvements to documentation and sample projects.

## Fixed
- Query functions that throw `OperationCancelledException` instead of `TaskCancelledException` will now be caught correctly after cancelling a query.

[Changes][v0.2.0]


<a name="v0.1.2"></a>
# [v0.1.2](https://github.com/Jcparkyn/phetch/releases/tag/v0.1.2) - 06 Aug 2022

## Changes
- Enabled source link, debug symbols, and deterministic builds.
- Added null checks to public methods, so they now throw `ArgumentNullException` if `null` is passed where it shouldn't be.

## Other
- Set up CI deployments with GitHub actions

[Changes][v0.1.2]


<a name="v0.1.1"></a>
# [v0.1.1](https://github.com/Jcparkyn/phetch/releases/tag/v0.1.1) - 01 Aug 2022

## Changes

- `Query.Cancel()` now causes the query state to change immediately, rather than waiting for the query function to throw a `TaskCanceledException`.

## New Features

- Added a `PrefetchAsync` method to `Endpoint`, which can be used to fetch data ahead of time.
- Added new overloads for `Endpoint` constructors which don't require a `CancellationToken` to be passed.
- Added a `Trigger()` method to `Query<T>` that doesn't require an argument to be passed.

## Bugfixes

- Fixed a bug which caused `Cancel()` to result in a query error if the cancellation didn't happen synchronously.

[Changes][v0.1.1]


<a name="v0.1.0"></a>
# [v0.1.0](https://github.com/Jcparkyn/phetch/releases/tag/v0.1.0) - 26 Jul 2022

This release removes the concept of mutations as the previously existed, and instead combines the previous functionality of mutations into the `Query` class. This is a very significant change, but future releases should contain far fewer breaking changes.

## BREAKING CHANGES:
- Removes the existing `Mutation` and `MutationEndpoint` classes.
  - You can now just use the standard `Query` classes, and call `Trigger` instead of `SetArg` to get the same functionality.
- Replaces `QueryEndpoint` with `Endpoint`, `ParameterlessEndpoint`, and 'MutationEndpoint` (depending on the shape of the query function).
- Replaces `UseQueryEndpoint` with `UseEndpoint`, `UseParameterlessEndpoint`, and 'UseMutationEndpoint`.
- Renames `SetParam` to `SetArg`.

## New Features
- Mutations now have all the same features as queries, including caching, cache updates/invalidation, and cancellation.
- Adds `OnSuccess`/`OnFailure` options to both queries and endpoints.
- Adds support for `CancellationToken` across the entire library.
- Adds a `Skip` parameter to `UseEndpoint`, for conditional fetching.
- Lots of extra documentation.

## Other Changes
- Fixes bugs involving the handling of out-of-order query returns.
- Fixes bugs causing double-fetching
- Fixes bugs causing unnecessary double-rendering of Blazor components.
- Adds lots of automated tests.
- Significantly improves the README.

[Changes][v0.1.0]


[v0.6.0]: https://github.com/Jcparkyn/phetch/compare/v0.5.1...v0.6.0
[v0.5.1]: https://github.com/Jcparkyn/phetch/compare/v0.5.0...v0.5.1
[v0.5.0]: https://github.com/Jcparkyn/phetch/compare/v0.4.0...v0.5.0
[v0.4.0]: https://github.com/Jcparkyn/phetch/compare/v0.3.1...v0.4.0
[v0.3.1]: https://github.com/Jcparkyn/phetch/compare/v0.3.0...v0.3.1
[v0.3.0]: https://github.com/Jcparkyn/phetch/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/Jcparkyn/phetch/compare/v0.1.2...v0.2.0
[v0.1.2]: https://github.com/Jcparkyn/phetch/compare/v0.1.1...v0.1.2
[v0.1.1]: https://github.com/Jcparkyn/phetch/compare/v0.1.0...v0.1.1
[v0.1.0]: https://github.com/Jcparkyn/phetch/tree/v0.1.0
