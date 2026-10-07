using ShopDemo.Tests.E2E.Pom.V1.Pages;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V1;

public abstract class PomPageTestBase : ShopDemoPageTest
{
    private PomPages? _pages;

    // Per-test initialization of the POM pages, using the current Playwright page to avoid sharing state between tests (no fixtures). This ensures that each test has a fresh instance of the POM pages.
    protected PomPages Pages => _pages ??= new PomPages(Page);
}
