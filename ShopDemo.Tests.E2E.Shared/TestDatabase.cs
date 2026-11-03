namespace ShopDemo.Tests.E2E.Shared;

/// <summary>
/// The application keeps its state in a real database, so end-to-end tests start from the seed data by calling
/// the server's test-support endpoint. Set <c>SHOPDEMO_RESET_DB=false</c> to keep the data between tests.
/// </summary>
public static class TestDatabase
{
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        // The local development server uses a self-signed certificate.
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static bool Enabled =>
        !string.Equals(Environment.GetEnvironmentVariable("SHOPDEMO_RESET_DB"), "false", StringComparison.OrdinalIgnoreCase);

    public static async Task ResetAsync()
    {
        if (!Enabled)
        {
            return;
        }

        var url = new Uri(new Uri(E2ETestSettings.BaseUrl), "api/test/reset");
        using var response = await Http.PostAsync(url, content: null);
        response.EnsureSuccessStatusCode();
    }
}
