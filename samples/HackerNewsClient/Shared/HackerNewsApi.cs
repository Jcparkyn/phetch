namespace HackerNewsClient.Shared;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Phetch.Core;

public class HackerNewsApi
{
    private readonly HttpClient _httpClient;

    public HackerNewsApi(HttpClient httpClient)
    {
        _httpClient = httpClient;

        var defaultOptions = new EndpointOptions()
        {
            DefaultStaleTime = TimeSpan.FromSeconds(60),
            RetryHandler = RetryHandler.Simple(1),
            OnFailure = e => Console.WriteLine(e.Exception.StackTrace),
        };

        GetItem = new(GetItemMethod, defaultOptions);

        GetTopStories = new(GetTopStoriesMethod, defaultOptions);

        GetUser = new(
            async (userId, ct) => (await httpClient.GetFromJsonAsync<HnUser>(
                $"https://hn.algolia.com/api/v1/users/{userId}",
                ct
            ))!,
            options: defaultOptions
        );
    }

    public async Task<HnItemDetails> GetItemMethod(int itemId, CancellationToken ct)
    {
        return (await _httpClient.GetFromJsonAsync<HnItemDetails>(
            $"https://hn.algolia.com/api/v1/items/{itemId}",
            ct
        ))!;
    }

    public async Task<SearchResponse<HnItem>> GetTopStoriesMethod(SearchStoriesArgs args, CancellationToken ct)
    {
        var queries = new Dictionary<string, string?>()
        {
            { "tags", args.Tag },
            { "query", args.Query },
            { "hitsPerPage", args.PageSize.ToString() },
            { "page", args.Page.ToString() },
        };

        if (args.StartDate is DateTimeOffset dto)
            queries.Add("numericFilters", $"created_at_i>{dto.ToUnixTimeSeconds()}");

        var url = QueryHelpers.AddQueryString("https://hn.algolia.com/api/v1/search", queries);

        var result = await _httpClient.GetFromJsonAsync<SearchResponse<HnItem>>(url, ct);
        return result!;
    }

    public Endpoint<SearchStoriesArgs, SearchResponse<HnItem>> GetTopStories { get; }

    public Endpoint<int, HnItemDetails> GetItem { get; }

    public Endpoint<string, HnUser> GetUser { get; }

    public record SearchStoriesArgs(
        int Page,
        int PageSize = 20,
        string? Query = "",
        string? Tag = "",
        DateTimeOffset? StartDate = null)
    {
        public SearchStoriesArgs GetNextPageArgs() => this with { Page = Page + 1 };
    }
}

public record SearchResponse<T>(
    List<T> Hits,
    int NbHits,
    int Page,
    int NbPages
);

public record HnUser(
    string Username,
    string About,
    int Karma
);

public record HnItem(
    [property: JsonPropertyName("objectID")] int Id,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    string Title,
    string? Url,
    string Author,
    int? Points,
    [property: JsonPropertyName("story_text")] string StoryText,
    [property: JsonPropertyName("num_comments")] int? NumComments
)
{
    public string? UrlDomain => string.IsNullOrEmpty(Url) ? null : new Uri(Url).Host;
};

public record HnItemDetails(
    int Id,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    string Title,
    string? Url,
    string? Author,
    int? Points,
    string? Text,
    string Type,
    List<HnItemDetails> Children
)
{
    public bool HasChildren => Children.Count > 0;

    public IEnumerable<HnItemDetails> ValidChildren { get; } = Children.Where(c => !string.IsNullOrEmpty(c.Text));

    private int? _totalChildCount;

    public int TotalChildCount => _totalChildCount ??= Children.Sum(static c => c.TotalChildCount) + Children.Count;
};

