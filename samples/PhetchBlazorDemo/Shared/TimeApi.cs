namespace PhetchBlazorDemo.Shared;

using Phetch.Core;

public class TimeApi
{
    public TimeApi(SimulateErrorService simulateErrorService)
    {
        TimeNowEndpoint = new(
            async (ct) =>
            {
                Console.WriteLine("Fetching time...");
                await Task.Delay(500);
                simulateErrorService.MaybeSimulateError();
                return DateTime.Now;
            });
    }

    public ParameterlessEndpoint<DateTime> TimeNowEndpoint { get; }
}

