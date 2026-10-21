using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ProductDetailsPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "product-details")]
    public required ILocator Root { get; init; }

    [Locator(LocatorKind.DataTestId, "product-overview-tab")]
    public required Tab OverviewTab { get; init; }

    [Locator(LocatorKind.DataTestId, "product-inventory-tab")]
    public required Tab InventoryTab { get; init; }

    public async Task<ProductDetailsOverviewTab> SwitchToOverviewTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Overview tab.");
        await OverviewTab.Select();
        var panel = Page.GetByTestId("product-overview-panel");

        E2ETrace.Write(typeof(ProductDetailsPage), "Creating the scoped Overview tab page object.");
        var overview = await CreatePageObject<ProductDetailsOverviewTab>(panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Overview tab page object is ready.");
        return overview;
    }

    public async Task<ProductDetailsInventoryTab> SwitchToInventoryTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Inventory tab.");
        await InventoryTab.Select();
        var panel = Page.GetByTestId("product-inventory-panel");

        E2ETrace.Write(
            typeof(ProductDetailsPage),
            "Creating the scoped Inventory tab page object; required locators may wait for loaded data.");
        var inventory = await CreatePageObject<ProductDetailsInventoryTab>(panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Inventory tab page object is ready.");
        return inventory;
    }
}
