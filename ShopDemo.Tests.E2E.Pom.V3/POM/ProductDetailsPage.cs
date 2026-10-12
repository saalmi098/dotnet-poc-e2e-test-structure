using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

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
        await OverviewTabLabel.Value.ClickAsync();
        var panel = _page.GetByTestId("product-overview-panel");
        return await Create<ProductDetailsOverviewTab>(_page, panel);
    }

    public async Task<ProductDetailsInventoryTab> SwitchToInventoryTab()
    {
        await InventoryTabLabel.Value.ClickAsync();
        var panel = _page.GetByTestId("product-inventory-panel");
        return await Create<ProductDetailsInventoryTab>(_page, panel);
    }
}
