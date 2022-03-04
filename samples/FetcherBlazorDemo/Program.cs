using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FetcherBlazorDemo;
using MudBlazor.Services;
using Blazored.LocalStorage;
using FetcherBlazorDemo.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services
    .AddMudServices()
    .AddBlazoredLocalStorage();
builder.Services.AddScoped<IsEvenApi>();

await builder.Build().RunAsync();
