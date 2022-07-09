namespace PhetchBlazorDemo.Shared;

using System.Net.Http.Json;
using Phetch.Core;

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

    public QueryEndpoint<int, bool> IsEven { get; }
}

public record IsEvenResponse(bool IsEven);
