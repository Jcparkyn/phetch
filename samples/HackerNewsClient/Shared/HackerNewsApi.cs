namespace HackerNewsClient.Shared;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Phetch;

public class HackerNewsApi
{
    public HackerNewsApi(HttpClient httpClient)
    {
        GetItem = new(
            async (itemId) => (await httpClient.GetFromJsonAsync<HnItemDetails>(
                $"https://hn.algolia.com/api/v1/items/{itemId}"
            ))!
        );

        GetTopStories = new(
            async _ => (await httpClient.GetFromJsonAsync<SearchResponse<HnItem>>(
                $"https://hn.algolia.com/api/v1/search?tags=front_page"
            ))!
        );
    }

    public ApiMethod<Unit, SearchResponse<HnItem>> GetTopStories { get; }

    public ApiMethod<int, HnItemDetails> GetItem { get; }
}

public enum HnItemType
{
    Story,
    Coment,
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
    [property: JsonPropertyName("num_comments")] int NumComments
);

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
);

