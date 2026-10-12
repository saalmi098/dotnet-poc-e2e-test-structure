using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class InventoryPage : PageComponent, IPageComponent<InventoryPage>
{
    public Required<ILocator> Root { get; }

    private InventoryPage(IPage page)
    {
        Root = Require(page.GetByTestId("inventory-page"), $"{nameof(InventoryPage)}.{nameof(Root)}");
    }

    static InventoryPage IPageComponent<InventoryPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task UpdateStock(string productId, int stock)
    {
        var item = Root.Value.GetByTestId($"inventory-item-{productId}");
        await item.GetByTestId($"inventory-stock-{productId}").Locator("input").FillAsync(stock.ToString());
        await item.GetByTestId($"inventory-save-{productId}").ClickAsync();
        await item.GetByTestId($"inventory-current-stock-{productId}")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }
}
