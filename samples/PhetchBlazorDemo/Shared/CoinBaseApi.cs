namespace PhetchBlazorDemo.Shared;

using System.Net.Http.Json;
using Phetch;

public class CoinbaseApi
{
    public CoinbaseApi(HttpClient httpClient)
    {
        GetTopAssets = new(
            pageNum => httpClient.GetFromJsonAsync<ApiResponse>(
                $"https://www.coinbase.com/api/v2/assets/search?filter=all&include_prices=true&limit=10&order=asc&page={pageNum}&query=&resolution=day&sort=rank"
            )
        );
    }

    public QueryEndpoint<int, ApiResponse?> GetTopAssets { get; }

    public record ApiResponse(
        PaginationData Pagination,
        List<Currency> Data
    );

    public record Currency(string Id, string Name, decimal Latest);

    public record PaginationData(
        int total_pages
    );
}
