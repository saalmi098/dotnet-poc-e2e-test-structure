using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V1.Pages;

public sealed class OrdersPage(IPage page)
{
    public Task OpenAsync() => page.GetByTestId("nav-orders-desktop").ClickAsync();

    public async Task<bool> ContainsOrderAsync(string orderNumber) =>
        await page.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0;
}
