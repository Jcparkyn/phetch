namespace FetcherBlazorDemo.Shared;

using System.Net.Http.Json;
using Fetcher;

public class IsEvenApi
{
    public IsEvenApi(HttpClient httpClient)
    {
        IsEven = new(
            async val => (await httpClient.GetFromJsonAsync<IsEvenResponse>(
                $"https://api.isevenapi.xyz/api/iseven/{val}"
            ))!.IsEven
        );
    }

    public ApiMethod<int, bool> IsEven { get; }
}

public record IsEvenResponse(bool IsEven);
