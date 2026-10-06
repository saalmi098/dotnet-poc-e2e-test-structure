using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ShopDemo.Application;
using ShopDemo.Client;
using ShopDemo.Infrastructure;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddScoped<JsonProductCatalog>();
builder.Services.AddScoped<IProductCatalog>(services => services.GetRequiredService<JsonProductCatalog>());
builder.Services.AddScoped<IInventoryCatalog>(services => services.GetRequiredService<JsonProductCatalog>());
builder.Services.AddScoped<IDemoAuthService, DemoAuthService>();
builder.Services.AddScoped<ICartService, InMemoryCartService>();
builder.Services.AddScoped<IOrderStore, InMemoryOrderStore>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IInventoryManagementService, InventoryManagementService>();

await builder.Build().RunAsync();
