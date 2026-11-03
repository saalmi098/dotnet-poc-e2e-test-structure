using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShopDemo.Domain;
using ShopDemo.Infrastructure;
using ShopDemo.Infrastructure.Data;
using ShopDemo.Server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Serve the Blazor client's static assets also when started with `dotnet run` outside the Development environment.
builder.WebHost.UseStaticWebAssets();

var provider = builder.Configuration.GetValue("Database:Provider", DatabaseProvider.Sqlite);
var connectionString = builder.Configuration.GetConnectionString("ShopDb") ?? "Data Source=App_Data/shopdemo.db";
if (provider == DatabaseProvider.Sqlite)
{
    EnsureDatabaseDirectory(connectionString);
}

builder.Services.AddShopInfrastructure(connectionString, provider);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "shopdemo.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // This is an API: answer 401/403 instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Manager, policy => policy.RequireRole(nameof(UserRole.StoreManager)));

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, detail) = exception switch
    {
        ShopException shop => (shop.StatusCode, shop.Message),
        OperationCanceledException => (StatusCodes.Status400BadRequest, "The request was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    if (status == StatusCodes.Status500InternalServerError)
    {
        app.Logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
    }

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = ReasonPhrase(status),
        Detail = detail
    });
}));

await InitializeDatabaseAsync(app);

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Ok(new { status = "ok" }));
api.MapAuthEndpoints();
api.MapCatalogEndpoints();
api.MapWishlistEndpoints();
api.MapCartEndpoints();
api.MapOrderEndpoints();
api.MapAdminEndpoints();
if (app.Configuration.GetValue("Testing:EnableResetEndpoint", true))
{
    api.MapTestSupportEndpoints();
}

api.Map("/{**rest}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));

app.MapFallbackToFile("index.html");

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    // The database container may still be starting up; retry for a while before giving up.
    const int maxAttempts = 30;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().InitializeAsync();
            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, exception.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

static void EnsureDatabaseDirectory(string connectionString)
{
    var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static string ReasonPhrase(int status) => status switch
{
    400 => "Bad Request",
    401 => "Unauthorized",
    402 => "Payment Required",
    403 => "Forbidden",
    404 => "Not Found",
    409 => "Conflict",
    _ => "Server Error"
};

public partial class Program;
