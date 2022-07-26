namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

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
        var query = endpoint.Use(Options);
        if (HasSetArg && !Skip)
        {
            query.SetArg(Arg);
        }
        return query;
    }
}
