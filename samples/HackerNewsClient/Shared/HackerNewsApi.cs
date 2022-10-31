namespace HackerNewsClient.Shared;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Phetch.Core;

public class HackerNewsApi
{
    public HackerNewsApi(HttpClient httpClient)
    {
        GetItem = new(
            async (itemId, ct) => (await httpClient.GetFromJsonAsync<HnItemDetails>(
                $"https://hn.algolia.com/api/v1/items/{itemId}",
                ct
            ))!,
            options: new()
            {
                DefaultStaleTime = TimeSpan.FromSeconds(60),
            }
        );

        GetTopStories = new(
            async (args, ct) =>
            {
                var url = $"https://hn.algolia.com/api/v1/search?tags={args.Tag}&query={args.Query}&hitsPerPage={args.PageSize}&page={args.Page}";// &query={args.Query}";
                if (args.StartDate is DateTimeOffset dto)
                {
                    url += $"&numericFilters=created_at_i>{dto.ToUnixTimeSeconds()}";
                }
                return (await httpClient.GetFromJsonAsync<SearchResponse<HnItem>>(url, ct))!;
            },
            options: new()
            {
                DefaultStaleTime = TimeSpan.FromMinutes(2),
            }
        );
    }

    public Endpoint<GetTopStoriesArgs, SearchResponse<HnItem>> GetTopStories { get; }

    public Endpoint<int, HnItemDetails> GetItem { get; }

    public record GetTopStoriesArgs(int Page, int PageSize = 20, string Query = "", string Tag = "", DateTimeOffset? StartDate = null)
    {
        public GetTopStoriesArgs GetNextPageArgs() => this with { Page = Page + 1 };
    }
}

public record SearchResponse<T>(
    List<T> Hits,
    int NbHits,
    int Page,
    int NbPages
);

public record HnItem(
    [property: JsonPropertyName("objectID")] int Id,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
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
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    string Title,
    string? Url,
    string Author,
    int? Points,
    string? Text,
    string Type,
    List<HnItemDetails> Children
)
{
    public bool HasChildren => Children.Count > 0;
};

