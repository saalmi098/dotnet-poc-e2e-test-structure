using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class ProductDetailsPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("product-details")]
    public ILocator Root { get; private set; } = null!;

    [DataTestId("product-overview-tab")]
    public ILocator OverviewTabLabel { get; private set; } = null!;

    [DataTestId("product-inventory-tab")]
    public ILocator InventoryTabLabel { get; private set; } = null!;

    public async Task<ProductDetailsOverviewTab> SwitchToOverviewTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Overview tab.");
        await OverviewTabLabel.ClickAsync();
        var panel = Page.GetByTestId("product-overview-panel");

        E2ETrace.Write(typeof(ProductDetailsPage), "Creating the scoped Overview tab page object.");
        var overview = await Create<ProductDetailsOverviewTab>(panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Overview tab page object is ready.");
        return overview;
    }

    public async Task<ProductDetailsInventoryTab> SwitchToInventoryTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Inventory tab.");
        await InventoryTabLabel.ClickAsync();
        var panel = Page.GetByTestId("product-inventory-panel");

        E2ETrace.Write(
            typeof(ProductDetailsPage),
            "Creating the scoped Inventory tab page object; required locators may wait for loaded data.");
        var inventory = await Create<ProductDetailsInventoryTab>(panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Inventory tab page object is ready.");
        return inventory;
    }
}
