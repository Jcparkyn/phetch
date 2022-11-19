namespace PhetchBlazorDemo.Shared;

using System.Net;
using System.Net.Http.Json;
using Phetch.Core;

public class IsEvenApi
{
    public IsEvenApi(HttpClient httpClient)
    {
        IsEven = new(
            async (val, ct) => (await httpClient.GetFromJsonAsync<IsEvenResponse>(
                $"https://api.isevenapi.xyz/api/iseven/{WebUtility.UrlEncode(val.ToString())}",
                ct
            ))!.IsEven
        );
    }

    public Endpoint<int, bool> IsEven { get; }
}

public record IsEvenResponse(bool IsEven);
