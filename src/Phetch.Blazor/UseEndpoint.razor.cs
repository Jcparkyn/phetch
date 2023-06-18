namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

/// <summary>
/// A component that can be used to call an endpoint and access the result.
/// </summary>
/// <remarks>
/// If you're calling a <see cref="Phetch.Core.ParameterlessEndpoint{TResult}"/>, you should use <see
/// cref="UseParameterlessEndpoint{TResult}"/> instead.
/// </remarks>
public partial class UseEndpoint<TArg, TResult> : UseEndpointWithArg<TArg, TResult>
{
    /// <summary>
    /// The endpoint to use.
    /// </summary>
    [Parameter, EditorRequired]
    public Endpoint<TArg, TResult> Endpoint
    {
        get => base.EndpointInternal!;
        set
        {
            if (ReferenceEquals(base.EndpointInternal, value))
                return;
            base.EndpointInternal = value;
            UpdateQuery();
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Query<TArg, TResult>> ChildContent { get; set; } = null!;

    protected override Query<TArg, TResult> CreateQuery(Endpoint<TArg, TResult> endpoint)
    {
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        var query = endpoint.Use(Options);
        if (HasSetArg && !Skip)
        {
            query.SetArg(Arg);
        }
        return query;
    }
}

[Obsolete("Use <UseEndpoint/> instead, which functions identically. This will be removed in a future version of Phetch.")]
public sealed class UseMutationEndpoint<TArg> : UseEndpoint<TArg, Unit> { }
