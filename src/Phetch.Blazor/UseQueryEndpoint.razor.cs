namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

public sealed partial class UseQueryEndpoint<TArg, TResult>
{
    private Query<TArg, TResult>? _query;
    private QueryEndpoint<TArg, TResult>? _endpoint;

    [Parameter, EditorRequired]
    public QueryEndpoint<TArg, TResult>? Endpoint
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
    public RenderFragment<Query<TArg, TResult>> ChildContent { get; set; } = null!;

    /// <summary>
    /// If set to true, any exceptions from the query will be re-thrown during rendering. This
    /// allows them to be caught by an <see cref="ErrorBoundary"/> further up the component hierarchy.
    /// </summary>
    [Parameter]
    public bool UseErrorBoundary { get; set; } = false;

    // Other parameters can be set before Param is set, so don't use Param until it has been set.
    // A nullable here would not work for queries where null is a valid argument.
    private bool _hasSetParam = false;
    private TArg _param = default!;

    [Parameter, EditorRequired]
    public TArg Param
    {
        get => _param;
        set
        {
            _param = value;
            _hasSetParam = true;
            _query?.SetParam(value);
        }
    }

    private QueryOptions<TResult>? _options;
    [Parameter]
    public QueryOptions<TResult>? Options
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

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_query);
    }

    private Query<TArg, TResult> GetQuery(QueryEndpoint<TArg, TResult> endpoint, QueryOptions<TResult>? options)
    {
        var newQuery = options is null ? endpoint.Use() : endpoint.Use(options);
        newQuery.StateChanged += StateHasChanged;
        if (_hasSetParam)
            newQuery.SetParam(_param);
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
}
