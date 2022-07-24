namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public partial class UseMutationEndpoint<TArg> : UseEndpointWithArg<TArg, Unit>
{
    /// <summary>
    /// The endpoint to use.
    /// </summary>
    [Parameter, EditorRequired]
    public MutationEndpoint<TArg>? Endpoint
    {
        get => (MutationEndpoint<TArg>?)_endpoint;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            _endpoint = value;
            UpdateQuery();
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Mutation<TArg>> ChildContent { get; set; } = null!;

    protected override Query<TArg, Unit> CreateQuery(Endpoint<TArg, Unit> endpoint)
    {
        var query = ((MutationEndpoint<TArg>)endpoint).Use(Options);
        if (_hasSetArg)
        {
            query.SetArg(_arg);
        }
        return query;
    }
}
