namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseParameterlessEndpoint<TResult>
    : IDisposable, IUseEndpoint<Unit, TResult>
{
    private Query<TResult>? _query;
    private ParameterlessEndpoint<TResult>? _endpoint;
    private bool _isInitialized;

    [Parameter, EditorRequired]
    public ParameterlessEndpoint<TResult>? Endpoint
    {
        get => _endpoint;
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
            _options = value;
            UpdateQuery();
        }
    }

    /// <inheritdoc/>
    [Parameter]
    public Action<QuerySuccessContext<Unit, TResult>>? OnSuccess { get; set; }

    /// <inheritdoc/>
    [Parameter]
    public Action<QueryFailureContext<Unit>>? OnFailure { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _isInitialized = true;
        UpdateQuery();
    }

    private void UpdateQuery()
    {
        if (!_isInitialized)
            return;
        TryUnsubscribe(_query);
        _query = GetQuery(_endpoint!, _options);
    }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Query<TResult> GetQuery(ParameterlessEndpoint<TResult> endpoint, QueryOptions<Unit, TResult>? options)
    {
        var newQuery = endpoint.Use(options);
        if (AutoFetch)
            newQuery.Fetch();
        newQuery.StateChanged += StateHasChanged;
        newQuery.Succeeded += SuccessCallback;
        newQuery.Failed += FailureCallback;
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
