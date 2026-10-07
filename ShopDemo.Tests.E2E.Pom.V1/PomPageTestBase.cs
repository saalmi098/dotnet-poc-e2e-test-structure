using ShopDemo.Tests.E2E.Pom.V1.Pages;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V1;

public abstract class PomPageTestBase : ShopDemoPageTest
{
    private PomPages? _pages;

    protected PomPages Pages => _pages ??= new PomPages(Page);
}
