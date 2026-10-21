using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ShopNavigation(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.Css, ".mud-appbar")]
    public required NavigationBar Bar { get; init; }
}

public sealed class NavigationBar(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "nav-catalog-desktop")]
    public required Button CatalogLink { get; init; }

    [Locator(LocatorKind.DataTestId, "nav-cart-desktop")]
    public required Button CartLink { get; init; }

    [Locator(LocatorKind.DataTestId, "nav-login-desktop", Optional = true)]
    public required Button LoginLink { get; init; }

    [Locator(LocatorKind.DataTestId, "nav-inventory-desktop", Optional = true)]
    public required Button InventoryLink { get; init; }

    [Locator(LocatorKind.DataTestId, "nav-orders-desktop", Optional = true)]
    public required Button OrdersLink { get; init; }

    [Locator(LocatorKind.DataTestId, "nav-logout-desktop", Optional = true)]
    public required Button LogoutLink { get; init; }

    public async Task<LoginPage> OpenLogin()
    {
        await LoginLink.Click();
        return await CreatePageObject<LoginPage>();
    }

    public async Task<CatalogPage> OpenCatalog()
    {
        await CatalogLink.Click();
        return await CreatePageObject<CatalogPage>();
    }

    public async Task<CartPage> OpenCart()
    {
        await CartLink.Click();
        return await CreatePageObject<CartPage>();
    }

    public async Task<InventoryPage> OpenInventory()
    {
        await InventoryLink.Click();
        return await CreatePageObject<InventoryPage>();
    }

    public async Task<OrdersPage> OpenOrders()
    {
        await OrdersLink.Click();
        return await CreatePageObject<OrdersPage>();
    }

    public async Task<ShopNavigation> SignOut()
    {
        await LogoutLink.Click();
        await LoginLink.Expect().ToBeVisibleAsync();
        return await CreatePageObject<ShopNavigation>();
    }
}
