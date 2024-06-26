﻿namespace PhetchBlazorDemo.Shared;

using Blazored.LocalStorage;
using Phetch.Core;

public class LocalStorageCounterApi
{
    public LocalStorageCounterApi(ILocalStorageService localStorage, SimulateErrorService simulateErrorService)
    {
        GetCounterValue = new(
            async ct =>
            {
                // More artificial delay
                await Task.Delay(Random.Shared.Next(1000), ct);
                simulateErrorService.MaybeSimulateError();
                return await localStorage.GetItemAsync<int>("counterVal", ct);
            }
        );
        SetCounterValue = new(
            async (val, ct) =>
            {
                // Add artificial delay to make the loading effect visible (don't do this in a real application).
                await Task.Delay(Random.Shared.Next(1000), ct);
                simulateErrorService.MaybeSimulateError();
                await localStorage.SetItemAsync("counterVal", val, ct);
                // Automatically update the stored value in the cache for GetCounterValue.
                GetCounterValue.UpdateQueryData(new Unit(), val);
            }
        );
    }

    public ResultlessEndpoint<int> SetCounterValue { get; }
    public ParameterlessEndpoint<int> GetCounterValue { get; }
}

