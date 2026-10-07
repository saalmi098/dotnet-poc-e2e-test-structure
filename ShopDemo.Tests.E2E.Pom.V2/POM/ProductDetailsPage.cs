using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

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
        await OverviewTabLabel.ClickAsync();
        var panel = Page.GetByTestId("product-overview-panel");
        return await Create<ProductDetailsOverviewTab>(panel); // return the page object for the Overview tab panel
    }

    public async Task<ProductDetailsInventoryTab> SwitchToInventoryTab()
    {
        await InventoryTabLabel.ClickAsync();
        var panel = Page.GetByTestId("product-inventory-panel");
        return await Create<ProductDetailsInventoryTab>(panel); // return the page object for the Inventory tab panel
    }
}
