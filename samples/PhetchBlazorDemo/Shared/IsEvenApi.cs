namespace PhetchBlazorDemo.Shared;

using System.Net;
using System.Net.Http.Json;
using Phetch.Core;

public class IsEvenApi
{
    public IsEvenApi(HttpClient httpClient, SimulateErrorService simulateErrorService)
    {
        IsEvenEndpoint = new(
            async (val, ct) =>
            {
                var response = await httpClient.GetFromJsonAsync<IsEvenResponse>(
                    $"https://api.isevenapi.xyz/api/iseven/{WebUtility.UrlEncode(val.ToString())}",
                    ct
                );
                simulateErrorService.MaybeSimulateError();
                return response!.IsEven;
            });
    }

    public Endpoint<int, bool> IsEvenEndpoint { get; }
}

public record IsEvenResponse(bool IsEven);
