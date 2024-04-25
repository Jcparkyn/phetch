namespace PhetchBlazorDemo.Shared;

public class SimulateErrorService
{
    public double SimulateErrorRate { get; set; }

    public void MaybeSimulateError()
    {
        if (SimulateErrorRate >= 1.0 || Random.Shared.NextDouble() < SimulateErrorRate)
        {
            Console.WriteLine("Simulating error");
            throw new Exception("Simulated error for testing. Turn these off using the \"Simulate network errors\" option at the top right.");
        }
    }
}
