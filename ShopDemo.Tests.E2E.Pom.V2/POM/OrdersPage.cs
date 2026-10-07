using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class OrdersPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("orders-page")]
    public ILocator Root { get; private set; } = null!;

    public async Task<bool> ContainsOrder(string orderNumber) =>
        await Root.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0;
}
