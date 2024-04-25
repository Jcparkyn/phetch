namespace PhetchBlazorDemo.Shared;

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Phetch.Core;

public class EarthquakeApi
{
    private readonly HttpClient _httpClient;
    private readonly SimulateErrorService _simulateErrorService;

    public EarthquakeApi(HttpClient httpClient, SimulateErrorService simulateErrorService)
    {
        _httpClient = httpClient;
        _simulateErrorService = simulateErrorService;
        GetEarthquakesEndpoint = new(GetEarthquakes,
            options: new()
            {
                DefaultStaleTime = TimeSpan.FromSeconds(30),
            }
        );
    }

    public Endpoint<SearchArgs, SearchResponse> GetEarthquakesEndpoint { get; }

    public async Task<SearchResponse> GetEarthquakes(SearchArgs args, CancellationToken ct)
    {
        var queries = new Dictionary<string, string?>()
        {
            { "format", "geojson" },
            { "limit", args.PageSize.ToString(CultureInfo.InvariantCulture) },
            { "offset", ((args.Page - 1) * args.PageSize + 1).ToString(CultureInfo.InvariantCulture) },
        };
        var url = QueryHelpers.AddQueryString("https://earthquake.usgs.gov/fdsnws/event/1/query?format=geojson", queries);

        // This could be a separate endpoint with its own caching, but we're keeping it simple here.
        var countUrl = "https://earthquake.usgs.gov/fdsnws/event/1/count?format=geojson";
        var countTask = _httpClient.GetFromJsonAsync<CountResponse>(countUrl, ct);

        var result = await _httpClient.GetFromJsonAsync<SearchResponse>(url, ct)
            ?? throw new JsonException("API response was null");

        _simulateErrorService.MaybeSimulateError();
        return result with { TotalCount = (await countTask)!.Count };
    }

    public record SearchArgs(
        [property: JsonPropertyName("hitsPerPage")] int PageSize,
        [property: JsonPropertyName("page")] int Page
    );

    public record SearchResponse(
        List<Feature> Features,
        int TotalCount)
    {
        public int GetPageCount(int pageSize) => (TotalCount + pageSize - 1) / pageSize;
    }

    public record Feature(FeatureProperties Properties);

    public record FeatureProperties(
        [property: JsonPropertyName("mag")] decimal Magnitude,
        [property: JsonPropertyName("place")] string Place,
        [property: JsonPropertyName("url")] string Url);

    public record CountResponse(int Count);
}
