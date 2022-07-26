namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseMutationEndpoint<TArg> : UseEndpointWithArg<TArg, Unit>
{
    /// <summary>
    /// The endpoint to use.
    /// </summary>
    [Parameter, EditorRequired]
    public MutationEndpoint<TArg> Endpoint
    {
        get => (MutationEndpoint<TArg>)base.EndpointInternal!;
        set
        {
            if (ReferenceEquals(base.EndpointInternal, value))
                return;
            base.EndpointInternal = value;
            UpdateQuery();
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Mutation<TArg>> ChildContent { get; set; } = null!;

    protected override Query<TArg, Unit> CreateQuery(Endpoint<TArg, Unit> endpoint)
    {
        var query = ((MutationEndpoint<TArg>)endpoint).Use(Options);
        if (HasSetArg && !Skip)
        {
            query.SetArg(Arg);
        }
        return query;
    }
}
