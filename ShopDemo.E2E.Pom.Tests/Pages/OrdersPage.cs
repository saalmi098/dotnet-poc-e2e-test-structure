using Microsoft.Playwright;

namespace ShopDemo.E2E.Pom.Tests.Pages;

public sealed class OrdersPage(IPage page)
{
    public Task OpenAsync() => page.GetByTestId("nav-orders-desktop").ClickAsync();

    public async Task<bool> ContainsOrderAsync(string orderNumber) =>
        await page.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0;
}
