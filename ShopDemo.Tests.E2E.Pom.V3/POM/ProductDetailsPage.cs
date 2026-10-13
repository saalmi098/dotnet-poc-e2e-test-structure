using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class ProductDetailsPage : PageComponent, IPageComponent<ProductDetailsPage>
{
    private readonly IPage _page;

    public Required<ILocator> Root { get; }
    public Required<ILocator> OverviewTabLabel { get; }
    public Required<ILocator> InventoryTabLabel { get; }

    private ProductDetailsPage(IPage page)
    {
        _page = page;
        Root = Require(page.GetByTestId("product-details"), $"{nameof(ProductDetailsPage)}.{nameof(Root)}");
        OverviewTabLabel = Require(page.GetByTestId("product-overview-tab"), $"{nameof(ProductDetailsPage)}.{nameof(OverviewTabLabel)}");
        InventoryTabLabel = Require(page.GetByTestId("product-inventory-tab"), $"{nameof(ProductDetailsPage)}.{nameof(InventoryTabLabel)}");
    }

    static ProductDetailsPage IPageComponent<ProductDetailsPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task<ProductDetailsOverviewTab> SwitchToOverviewTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Overview tab.");

        await OverviewTabLabel.Value.ClickAsync();
        var panel = _page.GetByTestId("product-overview-panel");

        E2ETrace.Write(typeof(ProductDetailsPage), "Creating the scoped Overview tab page object.");
        var overview = await Create<ProductDetailsOverviewTab>(_page, panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Overview tab page object is ready.");
        return overview;
    }

    public async Task<ProductDetailsInventoryTab> SwitchToInventoryTab()
    {
        E2ETrace.Write(typeof(ProductDetailsPage), "Clicking Inventory tab.");

        await InventoryTabLabel.Value.ClickAsync();
        var panel = _page.GetByTestId("product-inventory-panel");

        E2ETrace.Write(
            typeof(ProductDetailsPage),
            "Creating the scoped Inventory tab page object; required locators may wait for loaded data.");
        var inventory = await Create<ProductDetailsInventoryTab>(_page, panel);

        E2ETrace.Write(typeof(ProductDetailsPage), "Inventory tab page object is ready.");
        return inventory;
    }
}
