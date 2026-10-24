using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ShopDemo.Tests.E2E.Otel;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;
using System.Diagnostics;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

// TODO: remove V4 from the name of the class (use default ShopDemoPageTest and integrate the changes from here)
public abstract class ShopDemoPageTestV4 : PageTest
{
    private readonly PageObjectFactory _factory = new();
    private Activity? _testActivity;

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

    /// <summary>
    /// Opens one root span per test, named after the test itself, so every <c>Control.TraceInteraction</c>
    /// span performed during the test (clicks, fills, selections, ...) nests under it as a child span instead
    /// of each becoming its own disconnected trace. Mirrors how a real frontend RUM integration groups
    /// interactions under one page-view/session trace.
    /// </summary>
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var testName = TestContext.Current?.Test?.TestDisplayName ?? GetType().Name;
        _testActivity = UserInteractionTelemetry.ActivitySource.StartActivity(testName, ActivityKind.Internal);
        UserInteractionTelemetry.SetTestRootActivity(Page, _testActivity);
    }

    public override async ValueTask DisposeAsync()
    {
        UserInteractionTelemetry.SetTestRootActivity(Page, null);
        _testActivity?.Dispose();
        await base.DisposeAsync();
    }
}

public abstract class ShopDemoPageTestV4<TPageObject> : ShopDemoPageTestV4
    where TPageObject : PageObject
{
    private TPageObject? _model;

    /// <summary>
    /// Gets the initial page object after test initialization has navigated and bound it.
    /// </summary>
    protected TPageObject Model => _model
        ?? throw new InvalidOperationException("The initial page object is not available before test initialization completes.");

    /// <summary>
    /// Gets the application-relative route to open before binding <see cref="Model"/>.
    /// </summary>
    protected abstract string PageUrl { get; }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync(PageUrl);
        _model = await CreatePageObject<TPageObject>();
    }
}
