# Endpoints without parameters and/or return values

Sometimes you will need to use query functions that either have no parameters, or no return value.

For endpoints without parameters, you can use the `ParameterlessEndpoint` class, which is a subclass of `Endpoint` that accepts no parameters.
In your components, use the `<UseParameterlessEndpoint/>` to call these endpoints.

Similarly, use the `ResultlessEndpoint` class for endpoints that return no value. This can be used with the normal `<UseEndpoint/>` component.

> :information_source: If you use these classes, you may notice that some methods and types use the `Unit` type. This is a type used by Phetch to represent the absence of a value, which allows all classes to derive from the base `Query<TArg, TResult>` and `Endpoint<TArg, TResult>` types.
