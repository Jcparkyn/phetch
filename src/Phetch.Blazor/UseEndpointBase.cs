namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

public abstract class UseEndpointBase<TArg, TResult>
    : ComponentBase, IDisposable, IUseEndpoint<TArg, TResult>
{
    protected Query<TArg, TResult>? _query;
    protected Endpoint<TArg, TResult>? _endpoint;
    protected bool _isInitialized;

    /// <inheritdoc/>
    [Parameter]
    public bool UseErrorBoundary { get; set; } = false;

    private QueryOptions<TArg, TResult>? _options;

    /// <inheritdoc/>
    [Parameter]
    public QueryOptions<TArg, TResult>? Options
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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _isInitialized = true;
        UpdateQuery();
    }

    protected void UpdateQuery()
    {
        if (_isInitialized)
        {
            TryUnsubscribe(_query);
            _query = GetQuery(_endpoint!, _options);
        }
    }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Query<TArg, TResult> GetQuery(Endpoint<TArg, TResult> endpoint, QueryOptions<TArg, TResult>? options)
    {
        var newQuery = CreateQuery(endpoint);
        newQuery.StateChanged += StateHasChanged;
        return newQuery;
    }

    private void TryUnsubscribe(Query<TArg, TResult>? query)
    {
        if (query is not null)
        {
            query.StateChanged -= StateHasChanged;
            query.Detach();
        }
    }

    protected abstract Query<TArg, TResult> CreateQuery(Endpoint<TArg, TResult> endpoint);
}

public abstract class UseEndpointWithArg<TArg, TResult> : UseEndpointBase<TArg, TResult>
{
    protected bool _hasSetArg = false;
    protected TArg _arg = default!;

    /// <summary>
    /// The argument to supply to the query. If not supplied, the query will not be run automatically.
    /// </summary>
    [Parameter]
    public TArg Arg
    {
        get => _arg;
        set
        {
            _arg = value;
            _hasSetArg = true;
            if (_isInitialized)
                _query?.SetArg(value);
        }
    }
}
