using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ProductDetailsOverviewTab(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "product-overview-name")]
    public ILocator Name { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "product-overview-description")]
    public ILocator Description { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "product-overview-category")]
    public ILocator Category { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "product-overview-price")]
    public ILocator Price { get; private set; } = null!;
}
