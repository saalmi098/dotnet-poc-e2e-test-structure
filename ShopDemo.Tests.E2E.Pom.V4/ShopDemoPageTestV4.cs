using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4;

// TODO: remove V4 from the name of the class (use default ShopDemoPageTest and integrate the changes from here)
public abstract class ShopDemoPageTestV4 : PageTest
{
    /// <summary>
    /// Gets the per-test factory used to create and bind page objects.
    /// </summary>
    protected virtual PageObjectFactory PageObjectFactory => new();

    /// <summary>
    /// Creates and binds a page object for the current <see cref="PageTest.Page"/>.
    /// </summary>
    protected Task<T> CreatePageObject<T>(ILocator? baseLocator = null) where T : PageObject
        => PageObjectFactory.Create<T>(Page, baseLocator);

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2ETestSettings.BaseUrl,
        ViewportSize = new ViewportSize { Width = 1365, Height = 900 }
    };
}

public abstract class ShopDemoPageTestV4<TPageObject> : ShopDemoPageTestV4
    where TPageObject : PageObject
{
    /// <summary>
    /// Gets the initial page object after test initialization has navigated and bound it.
    /// </summary>
    protected TPageObject Model
    {
        get => field
            ?? throw new InvalidOperationException("The initial page object is not available before test initialization completes.");
        private set;
    }

    /// <summary>
    /// Gets the application-relative route to open before binding <see cref="Model"/>.
    /// </summary>
    protected abstract string PageUrl { get; }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync(PageUrl); // TODO: in Basement this is not needed, is it? Does this make sense (thinking of sub-page objects that are not the root page object)?
        Model = await CreatePageObject<TPageObject>();
    }
}
