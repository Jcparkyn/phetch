namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseParameterlessEndpoint<TResult> : UseEndpointBase<Unit, TResult>
{
    [Parameter, EditorRequired]
    public ParameterlessEndpoint<TResult> Endpoint
    {
        get => (ParameterlessEndpoint<TResult>)_endpoint!;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            _endpoint = value;
            UpdateQuery();
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Query<TResult>> ChildContent { get; set; } = null!;

    /// <summary>
    /// If set to true (the default), the query will be run automatically when the component is initialized.
    /// </summary>
    [Parameter]
    public bool AutoFetch { get; set; } = true;

    protected override Query<Unit, TResult> CreateQuery(Endpoint<Unit, TResult> endpoint)
    {
        var query = ((ParameterlessEndpoint<TResult>)endpoint).Use(Options);
        if (AutoFetch)
        {
            query.SetArg(default);
        }
        return query;
    }
}
