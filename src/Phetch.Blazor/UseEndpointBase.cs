namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Phetch.Core;

public abstract class UseEndpointBase<TArg, TResult>
    : ComponentBase, IDisposable
{
    protected Query<TArg, TResult>? _query;
    protected Endpoint<TArg, TResult>? _endpoint;
    protected bool _isInitialized;

    /// <summary>
    /// If set to true, any exceptions from the query will be re-thrown during rendering. This
    /// allows them to be caught by an <see cref="ErrorBoundary"/> further up the component hierarchy.
    /// </summary>
    [Parameter]
    public bool UseErrorBoundary { get; set; } = false;

    private QueryOptions<TArg, TResult>? _options;

    /// <summary>
    /// Additional options to use for this query.
    /// </summary>
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
            _query = GetQuery(_endpoint!);
        }
    }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Query<TArg, TResult> GetQuery(Endpoint<TArg, TResult> endpoint)
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
    private bool _skip;

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
            if (_isInitialized && !Skip)
                _query?.SetArg(value);
        }
    }

    /// <summary>
    /// If true, the query will not be run automatically.
    /// This does not affect manual query invokations using methods on the Query object.
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
            if (_isInitialized && _hasSetArg && !value)
                _query?.SetArg(_arg);
        }
    }
}
