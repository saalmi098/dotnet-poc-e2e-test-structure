using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ShopDemo.Client;
using ShopDemo.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddScoped<ShopApi>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<CartState>();

var host = builder.Build();

// Restore the session cookie's user and the cart badge before the first render.
await host.Services.GetRequiredService<AuthState>().InitializeAsync();
try
{
    await host.Services.GetRequiredService<CartState>().RefreshAsync();
}
catch (ApiException)
{
    // The header shows an empty cart; the pages report API problems themselves.
}

await host.RunAsync();
