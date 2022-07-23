namespace Phetch.Blazor;

using System;
using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseMutationEndpoint<TArg>
    : IDisposable, IUseEndpoint<TArg, Unit>
{
    private Mutation<TArg>? _query;
    private MutationEndpoint<TArg>? _endpoint;

    /// <inheritdoc cref="UseEndpoint{TArg, TResult}.Endpoint"/>
    [Parameter, EditorRequired]
    public MutationEndpoint<TArg>? Endpoint
    {
        get => _endpoint;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            TryUnsubscribe(_query);
            if (value is not null)
            {
                _query = GetQuery(value, _options);
                _endpoint = value;
            }
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Mutation<TArg>> ChildContent { get; set; } = null!;

    /// <inheritdoc/>
    [Parameter]
    public bool UseErrorBoundary { get; set; } = false;

    // Other parameters can be set before Arg is set, so don't use Arg until it has been set. A
    // nullable here would not work for queries where null is a valid argument.
    private bool _hasSetArg = false;
    private TArg _arg = default!;

    /// <inheritdoc cref="UseEndpoint{TArg, TResult}.Arg"/>
    [Parameter]
    public TArg Arg
    {
        get => _arg;
        set
        {
            _arg = value;
            _hasSetArg = true;
            _query?.SetArg(value);
        }
    }

    private QueryOptions<TArg, Unit>? _options;

    /// <inheritdoc/>
    [Parameter]
    public QueryOptions<TArg, Unit>? Options
    {
        get => _options;
        set
        {
            if (_options == value)
                return;
            TryUnsubscribe(_query);
            if (_endpoint is not null)
            {
                _query = GetQuery(_endpoint, value);
            }

            _options = value;
        }
    }

    /// <inheritdoc/>
    [Parameter]
    public Action<QuerySuccessContext<TArg, Unit>>? OnSuccess { get; set; }

    /// <inheritdoc/>
    [Parameter]
    public Action<QueryFailureContext<TArg>>? OnFailure { get; set; }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Mutation<TArg> GetQuery(MutationEndpoint<TArg> endpoint, QueryOptions<TArg, Unit>? options)
    {
        var newQuery = endpoint.Use(options);
        newQuery.StateChanged += StateHasChanged;
        newQuery.Succeeded += SuccessCallback;
        newQuery.Failed += FailureCallback;
        if (_hasSetArg)
            newQuery.SetArg(_arg);
        return newQuery;
    }

    private void TryUnsubscribe(Mutation<TArg>? query)
    {
        if (query is not null)
        {
            query.StateChanged -= StateHasChanged;
            query.Succeeded -= SuccessCallback;
            query.Failed -= FailureCallback;
            query.Detach();
        }
    }

    private void SuccessCallback(QuerySuccessContext<TArg, Unit> context) { OnSuccess?.Invoke(context); }

    private void FailureCallback(QueryFailureContext<TArg> context) { OnFailure?.Invoke(context); }

}
