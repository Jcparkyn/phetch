namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseParameterlessEndpoint<TResult>
    : IDisposable, IUseEndpoint<Unit, TResult>
{
    private Query<TResult>? _query;
    private ParameterlessEndpoint<TResult>? _endpoint;

    [Parameter, EditorRequired]
    public ParameterlessEndpoint<TResult>? Endpoint
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
    public RenderFragment<Query<TResult>> ChildContent { get; set; } = null!;

    /// <inheritdoc/>
    [Parameter]
    public bool UseErrorBoundary { get; set; } = false;

    /// <summary>
    /// If set to true (the default), the query will be run automatically when the component is initialized.
    /// </summary>
    [Parameter]
    public bool AutoFetch { get; set; } = true;

    private QueryOptions<Unit, TResult>? _options;
    [Parameter]
    public QueryOptions<Unit, TResult>? Options
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
    public Action<QuerySuccessContext<Unit, TResult>>? OnSuccess { get; set; }

    /// <inheritdoc/>
    [Parameter]
    public Action<QueryFailureContext<Unit>>? OnFailure { get; set; }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Query<TResult> GetQuery(ParameterlessEndpoint<TResult> endpoint, QueryOptions<Unit, TResult>? options)
    {
        var newQuery = endpoint.Use(options);
        newQuery.StateChanged += StateHasChanged;
        newQuery.Succeeded += SuccessCallback;
        newQuery.Failed += FailureCallback;
        if (AutoFetch)
            newQuery.Fetch();
        return newQuery;
    }

    private void TryUnsubscribe(Query<TResult>? query)
    {
        if (query is not null)
        {
            query.StateChanged -= StateHasChanged;
            query.Succeeded -= SuccessCallback;
            query.Failed -= FailureCallback;
            query.Detach();
        }
    }

    private void SuccessCallback(QuerySuccessContext<Unit, TResult> context) { OnSuccess?.Invoke(context); }

    private void FailureCallback(QueryFailureContext<Unit> context) { OnFailure?.Invoke(context); }
}
