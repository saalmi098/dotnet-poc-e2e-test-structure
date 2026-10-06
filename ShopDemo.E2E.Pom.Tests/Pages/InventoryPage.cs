using Microsoft.Playwright;

namespace ShopDemo.E2E.Pom.Tests.Pages;

public sealed class InventoryPage(IPage page)
{
    public Task OpenDirectlyAsync() => page.GotoAsync("/manage/inventory");

    public async Task OpenAsync()
    {
        await page.GetByTestId("nav-inventory-desktop").ClickAsync();
        await page.GetByTestId("inventory-page").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public async Task UpdateStockAsync(string productId, int stock)
    {
        var item = page.GetByTestId($"inventory-item-{productId}");
        await item.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await item.GetByTestId($"inventory-stock-{productId}")
            .Locator("input")
            .FillAsync(stock.ToString());
        await item.GetByTestId($"inventory-save-{productId}").ClickAsync();
        await item.GetByTestId($"inventory-current-stock-{productId}")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    public Task<bool> IsVisibleAsync() => page.GetByTestId("inventory-page").IsVisibleAsync();

    public Task<bool> IsManagementNavigationVisibleAsync()
        => page.GetByTestId("nav-inventory-desktop").IsVisibleAsync();
}
