namespace PhetchBlazorDemo.Shared;

using Blazored.LocalStorage;
using Phetch.Core;

/// <summary>
/// An API for managing boats. This is a fake API that uses local storage, rather than making HTTP requests.
/// </summary>
public class LocalStorageBoatApi
{
    private readonly ILocalStorageService _localStorage;
    private readonly SimulateErrorService _simulateErrorService;
    private const string LocalStorageKey = "boats";

    public Endpoint<long, Boat?> GetBoatByIdEndpoint { get; set; }
    public ParameterlessEndpoint<List<Boat>> GetBoatsEndpoint { get; set; }
    public Endpoint<UpdateBoatModel, Boat> AddOrUpdateEndpoint { get; set; }
    public ResultlessEndpoint<long> DeleteEndpoint { get; set; }

    public LocalStorageBoatApi(ILocalStorageService localStorage, SimulateErrorService simulateErrorService)
    {
        _localStorage = localStorage;
        _simulateErrorService = simulateErrorService;
        var defaultOptions = new EndpointOptions
        {
            DefaultStaleTime = TimeSpan.FromMinutes(5),
        };
        GetBoatByIdEndpoint = new(GetBoatById, defaultOptions);
        GetBoatsEndpoint = new(GetBoats, options: new(defaultOptions)
        {
            OnSuccess = e =>
            {
                // This is an optional optimization to preload GetBoatByIdEndpoint with the information for each boat received.
                // This means the values are loaded instantly when the user clicks "edit" on a boat.
                foreach (var boat in e.Result)
                {
                    GetBoatByIdEndpoint.UpdateQueryData(boat.Id, boat, addIfNotExists: true);
                }
            }
        });
        AddOrUpdateEndpoint = new(AddOrUpdate, options: new(defaultOptions)
        {
            OnSuccess = e =>
            {
                GetBoatByIdEndpoint.UpdateQueryData(e.Result.Id, e.Result, addIfNotExists: true);
                GetBoatsEndpoint.InvalidateAll();
            }
        });
        DeleteEndpoint = new(Delete, options: new(defaultOptions)
        {
            OnSuccess = e =>
            {
                GetBoatByIdEndpoint.Invalidate(e.Arg);
                GetBoatsEndpoint.InvalidateAll();
            }
        });
    }

    private async Task<List<Boat>> LoadData()
    {
        var boats = await _localStorage.GetItemAsync<List<Boat>>(LocalStorageKey);
        if (boats is null)
        {
            // Default data
            return [
                new(2010462706893676885, "The Santa Maria", 19m),
                new(2927609302809486320, "H.L. Hunley", 12m),
                new(3480673129356434945, "The Mayflower", 30m),
                new(4202974600260265142, "USS Constitution", 63m),
                new(4589015296537932172, "USS Arizona", 185.3m),
                new(4880298856219381773, "Bismarck", 251m),
                new(8177348010691240713, "The Essex", 26.7m),
                new(8229626701702790468, "RMS Titanic", 269.1m),
            ];
        }
        return boats;
    }

    private async Task<List<Boat>> GetBoats()
    {
        await FakeDelay();
        var boats = await LoadData();
        return boats.OrderBy(x => x.Name).ToList();
    }

    private async Task<Boat?> GetBoatById(long id)
    {
        await FakeDelay();
        var boats = await LoadData();
        return boats.FirstOrDefault(t => t.Id == id);
    }

    private async Task<Boat> AddOrUpdate(UpdateBoatModel boat)
    {
        await FakeDelay();
        var boats = await LoadData();
        var id = boat.Id ?? Random.Shared.NextInt64();
        var newBoat = new Boat(id, boat.Name, boat.Length);
        if (boat.Id is null)
        {
            boats.Add(newBoat);
            await _localStorage.SetItemAsync(LocalStorageKey, boats);
            return newBoat;
        }
        var index = boats.FindIndex(t => t.Id == boat.Id);
        if (index >= 0)
        {
            boats[index] = newBoat;
            await _localStorage.SetItemAsync(LocalStorageKey, boats);
            return newBoat;
        }
        else
        {
            throw new InvalidOperationException($"Boat with ID {boat.Id} not found.");
        }
    }

    private async Task Delete(long id)
    {
        await FakeDelay();
        var boats = await LoadData();
        var index = boats.FindIndex(t => t.Id == id);
        if (index >= 0)
        {
            boats.RemoveAt(index);
        }
        await _localStorage.SetItemAsync(LocalStorageKey, boats);
    }

    // Fake delay to simulate a slow network request.
    private async Task FakeDelay()
    {
        Console.WriteLine("Pretending to make a network request...");
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.NextDouble() + 0.5));
        _simulateErrorService.MaybeSimulateError();
    }

    public record Boat(long Id, string Name, decimal Length);
    public record UpdateBoatModel(long? Id, string Name, decimal Length);
}
