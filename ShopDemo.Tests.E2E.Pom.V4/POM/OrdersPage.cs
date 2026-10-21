using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class OrdersPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "orders-page")]
    public required ILocator Root { get; init; }

    public async Task<bool> ContainsOrder(string orderNumber) =>
        await Root.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0;
}
