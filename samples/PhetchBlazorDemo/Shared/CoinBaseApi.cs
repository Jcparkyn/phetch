namespace PhetchBlazorDemo.Shared;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Phetch.Core;

public class CoinbaseApi
{
    public CoinbaseApi(HttpClient httpClient)
    {
        GetTopAssets = new(
            (pageNum, ct) => httpClient.GetFromJsonAsync<ApiResponse>(
                $"https://www.coinbase.com/api/v2/assets/search?filter=all&include_prices=true&limit=10&order=asc&page={pageNum}&query=&resolution=day&sort=rank",
                ct
            ),
            options: new()
            {
                DefaultStaleTime = TimeSpan.FromSeconds(30),
            }
        );
    }

    public Endpoint<int, ApiResponse?> GetTopAssets { get; }

    public record ApiResponse(
        PaginationData Pagination,
        List<Currency> Data
    );

    public record Currency(string Id, string Name, decimal Latest);

    public record PaginationData(
        [property: JsonPropertyName("total_pages")] int TotalPages
    );
}
