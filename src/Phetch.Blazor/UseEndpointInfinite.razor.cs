namespace Phetch.Blazor;

using Microsoft.AspNetCore.Components;
using Phetch.Core;

/// <summary>
/// An experimental component for creating "infinite scroll" features.
/// </summary>
/// <typeparam name="TArg"></typeparam>
/// <typeparam name="TResult"></typeparam>
// [RequiresPreviewFeatures("This component is experimental and may not work as intended.")]
public sealed partial class UseEndpointInfinite<TArg, TResult> : ComponentBase, IDisposable
{
    private Endpoint<TArg, TResult> _endpoint = null!;

    private readonly List<Query<TArg, TResult>> _queries = new();

    private bool _isInitialized;

    /// <summary>
    /// The endpoint to use.
    /// </summary>
    [Parameter, EditorRequired]
    public Endpoint<TArg, TResult> Endpoint
    {
        get => _endpoint!;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            _endpoint = value;
            ResetQueries();
        }
    }

    [Parameter]
    public RenderFragment<UseEndpointInfiniteContext<TArg, TResult>>? ChildContent { get; set; }

    private TArg? _arg;

    /// <summary>
    /// The argument to supply to the query. If not supplied, the query will not be run automatically.
    /// </summary>
    [Parameter]
    public TArg Arg
    {
        get => _arg!;
        set
        {
            if (EqualityComparer<TArg>.Default.Equals(_arg, value))
                return;
            _arg = value;
            if (_isInitialized)
                UpdateQueries();
        }
    }

    /// <summary>
    /// A function to choose the Arg for the next page, based on the last page and number of pages.
    /// </summary>
    [Parameter, EditorRequired]
    public GetNextPageFunc GetNextPageArg { get; set; } = null!;

    // Using an explicit delegate here, because otherwise the compiler doesn't understand nullable return type
    public delegate TArg? GetNextPageFunc(Query<TArg, TResult> lastPage, int pageCount);

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
            ResetQueries();
        }
    }

    public async Task LoadNextPageAsync()
    {
        var lastQuery = _queries.LastOrDefault();
        if (lastQuery is null)
        {
            // This might make more sense as a throw, it shouldn't be possible to get here in normal use.
            ResetQueries();
            return;
        }
        if (!lastQuery.IsSuccess)
        {
            return;
        }
        var nextArg = GetNextPageArg(lastQuery, _queries.Count);
        if (nextArg is null)
        {
            return;
        }
        var newQuery = GetQuery(_endpoint);
        _queries.Add(newQuery);
        // TODO: Return SetArgAsync result
        await newQuery.SetArgAsync(nextArg).ConfigureAwait(false);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _isInitialized = true;
        ResetQueries();
    }

    private void ResetQueries()
    {
        if (_isInitialized)
        {
            foreach (var query in _queries)
            {
                Unsubscribe(query);
            }
            _queries.Clear();
            _queries.Add(GetQuery(_endpoint));
            UpdateQueries();
        }
    }

    private void UpdateQueries()
    {
        var nextArg = Arg;

        for (var i = 0; i < _queries.Count; i++)
        {
            var query = _queries[i];
            query.SetArg(nextArg);

            if (!query.IsSuccess)
            {
                RemoveQueriesAfter(i);
                return;
            }

            nextArg = GetNextPageArg(query, i + 1);
            if (nextArg is null)
            {
                RemoveQueriesAfter(i);
                return;
            }
            var nextIsCached = _endpoint!.GetCachedQuery(nextArg) is not null;

            var isLastPage = i == _queries.Count - 1;
            if (isLastPage && query.IsSuccess && nextIsCached)
            {
                var newQuery = GetQuery(_endpoint);
                _queries.Add(newQuery);
            }
        }
    }

    private void RemoveQueriesAfter(int index)
    {
        if (index >= _queries.Count)
        {
            return;
        }
        for (var i = index + 1; i < _queries.Count; i++)
        {
            Unsubscribe(_queries[i]);
        }
        _queries.RemoveRange(index + 1, _queries.Count - index - 1);
    }

    private Query<TArg, TResult> GetQuery(Endpoint<TArg, TResult> endpoint)
    {
        var newQuery = endpoint.Use(Options);
        newQuery.StateChanged += StateHasChanged;
        return newQuery;
    }

    private void Unsubscribe(Query<TArg, TResult> query)
    {
        if (query is not null)
        {
            query.StateChanged -= StateHasChanged;
            query.Detach();
        }
    }

    public void Dispose()
    {
        foreach (var query in _queries)
        {
            Unsubscribe(query);
        }
        _queries.Clear();
    }
}

public sealed record UseEndpointInfiniteContext<TArg, TResult>
{
    internal UseEndpointInfiniteContext(UseEndpointInfinite<TArg, TResult> component, List<Query<TArg, TResult>> pages)
    {
        _component = component;
        Pages = pages;
        var lastPage = pages.LastOrDefault();
        HasNextPage = lastPage is not null
            && lastPage.IsSuccess
            && component.GetNextPageArg(lastPage, pages.Count) != null;
    }

    private readonly UseEndpointInfinite<TArg, TResult> _component;

    public IReadOnlyList<Query<TArg, TResult>> Pages { get; } = null!;
    public bool HasNextPage { get; }
    public bool IsLoadingNextPage => Pages.Count > 0 && Pages[^1].IsFetching;

    public void LoadNextPage() => _ = _component.LoadNextPageAsync();
    public Task LoadNextPageAsync() => _component.LoadNextPageAsync();
}
