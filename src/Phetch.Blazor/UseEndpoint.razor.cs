namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

/// <summary>
/// A component that can be used to call an endpoint and access the result.
/// </summary>
/// <remarks>
/// If you're calling a <see cref="ParameterlessEndpoint{TResult}"/>, you should use <see
/// cref="UseParameterlessEndpoint{TResult}"/> instead.
/// </remarks>
public partial class UseEndpoint<TArg, TResult> : UseEndpointBase<TArg, TResult>
{
    private bool _hasSetArg;
    private TArg? _arg;
    private bool _skip;

    /// <summary>
    /// The argument to supply to the query. If not supplied, the query will not be run automatically.
    /// </summary>
    [Parameter]
    public TArg Arg
    {
        get => _arg!;
        set
        {
            _arg = value;
            _hasSetArg = true;
            if (IsInitialized && !Skip)
                CurrentQuery?.SetArg(value);
        }
    }

    /// <summary>
    /// If true, the query will not be run automatically.
    /// This does not affect manual query invocations using methods on the Query object.
    /// </summary>
    /// <remarks>
    /// This is useful for delaying queries until the data they depend on is available.
    /// If no value for <see cref="Arg"/> is provided, this has no effect.
    /// </remarks>
    [Parameter]
    public bool Skip
    {
        get => _skip;
        set
        {
            _skip = value;
            if (IsInitialized && _hasSetArg && !value)
                CurrentQuery?.SetArg(_arg!);
        }
    }

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
        if (_hasSetArg && !Skip)
        {
            query.SetArg(Arg);
        }
        return query;
    }
}

[Obsolete("Use <UseEndpoint/> instead, which functions identically. This will be removed in a future version of Phetch.")]
public sealed class UseMutationEndpoint<TArg> : UseEndpoint<TArg, Unit> { }
