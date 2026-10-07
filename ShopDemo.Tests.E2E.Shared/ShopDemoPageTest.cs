using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace ShopDemo.Tests.E2E.Shared;

public abstract class ShopDemoPageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2ETestSettings.BaseUrl,
        ViewportSize = new ViewportSize { Width = 1365, Height = 900 },
        ColorScheme = ColorScheme.Light
    };
}
