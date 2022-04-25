namespace HackerNewsClient.Shared;

using System.Net.Http.Json;
using Phetch;

public class HackerNewsApi
{
    public HackerNewsApi(HttpClient httpClient)
    {
        GetItem = new(
            async (itemId) => (await httpClient.GetFromJsonAsync<HnItem>(
                $"https://hacker-news.firebaseio.com/v0/item/{itemId}.json"
            ))!
        );

        GetTopStories = new(
            async _ => (await httpClient.GetFromJsonAsync<List<int>>(
                $"https://hacker-news.firebaseio.com/v0/topstories.json"
            ))!
        );
    }

    public ApiMethod<Unit, List<int>> GetTopStories { get; }

    public ApiMethod<int, HnItem> GetItem { get; }
}

public record HnItem(
    int Id,
    bool Deleted,
    string By,
    int Time,
    string Title,
    string Text
);
