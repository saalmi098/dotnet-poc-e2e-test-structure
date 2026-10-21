using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4;

public abstract class ShopDemoPageTestV4 : PageTest
{
    private readonly PageObjectFactory _factory = new();

    /// <summary>
    /// Creates and binds a page object for the current <see cref="PageTest.Page"/>.
    /// </summary>
    protected Task<T> CreatePageObject<T>(ILocator? baseLocator = null) where T : PageObject
        => _factory.Create<T>(Page, baseLocator);

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2ETestSettings.BaseUrl,
        ViewportSize = new ViewportSize { Width = 1365, Height = 900 }
    };
}
