namespace ShopDemo.E2E.Shared;

public static class E2ETestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("SHOPDEMO_BASE_URL") ?? "https://localhost:53246/";

    public static DemoAccount Shopper { get; } =
        new("demo@shopdemo.local", "ShopDemo123!");

    public static DemoAccount StoreManager { get; } =
        new("manager@shopdemo.local", "ManagerDemo123!");

    public const string CanvasToteId = "canvas-tote";
}

public sealed record DemoAccount(string Email, string Password);
