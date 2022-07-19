namespace Phetch.Blazor;

using System;
using Microsoft.AspNetCore.Components.Web;
using Phetch.Core;

public interface IUseEndpoint<TArg, TResult> : IObserveQuery<TArg, TResult>
{
    /// <summary>
    /// If set to true, any exceptions from the query will be re-thrown during rendering. This
    /// allows them to be caught by an <see cref="ErrorBoundary"/> further up the component hierarchy.
    /// </summary>
    bool UseErrorBoundary { get; set; }

    /// <summary>
    /// Additional options to use for this query.
    /// </summary>
    QueryOptions<TArg, TResult>? Options { get; set; }
}

public interface IObserveQuery<TArg, TResult>
{
    /// <summary>
    /// Callback function to call when the query succeeds. This will only be called if this
    /// component is still mounted when the query succeeds.
    /// </summary>
    Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; set; }

    /// <summary>
    /// Callback function to call when the query fails. This will only be called if this component
    /// is still mounted when the query fails.
    /// </summary>
    Action<QueryFailureContext<TArg>>? OnFailure { get; set; }
}
