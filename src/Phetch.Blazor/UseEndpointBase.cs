namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Phetch.Core;

public abstract class UseEndpointBase<TArg, TResult>
    : ComponentBase, IDisposable
{
    internal UseEndpointBase() { }

    protected Query<TArg, TResult>? CurrentQuery { get; private set; }
    protected Endpoint<TArg, TResult>? EndpointInternal { get; set; }
    protected bool IsInitialized { get; private set; }

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
        IsInitialized = true;
        UpdateQuery();
    }

    protected void UpdateQuery()
    {
        if (IsInitialized)
        {
            TryUnsubscribe(CurrentQuery);
            CurrentQuery = GetQuery(EndpointInternal!);
        }
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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            TryUnsubscribe(CurrentQuery);
        }

        CurrentQuery = null;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
