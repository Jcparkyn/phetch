using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PhetchBlazorDemo;
using PhetchBlazorDemo.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services
    .AddMudServices()
    .AddBlazoredLocalStorage();
builder.Services.AddScoped<IsEvenApi>();
builder.Services.AddScoped<CoinbaseApi>();
builder.Services.AddScoped<LocalStorageCounterApi>();

await builder.Build().RunAsync();
