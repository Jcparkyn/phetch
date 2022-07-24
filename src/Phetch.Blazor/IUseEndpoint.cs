namespace Phetch.Blazor;

using System;
using Microsoft.AspNetCore.Components.Web;
using Phetch.Core;

public interface IUseEndpoint<TArg, TResult>
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
