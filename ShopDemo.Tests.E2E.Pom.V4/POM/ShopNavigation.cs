using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ShopNavigation(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.Css, ".mud-appbar")]
    public NavigationBar Bar { get; private set; } = null!;
}

public sealed class NavigationBar(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "nav-catalog-desktop")]
    public Button CatalogLink { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "nav-cart-desktop")]
    public Button CartLink { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "nav-login-desktop", Required = false)]
    public Button LoginLink { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "nav-inventory-desktop", Required = false)]
    public Button InventoryLink { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "nav-orders-desktop", Required = false)]
    public Button OrdersLink { get; private set; } = null!;

    [Locator(LocatorKind.DataTestId, "nav-logout-desktop", Required = false)]
    public Button LogoutLink { get; private set; } = null!;

    public async Task<LoginPage> OpenLogin()
    {
        await LoginLink.Click();
        return await Create<LoginPage>();
    }

    public async Task<CatalogPage> OpenCatalog()
    {
        await CatalogLink.Click();
        return await Create<CatalogPage>();
    }

    public async Task<CartPage> OpenCart()
    {
        await CartLink.Click();
        return await Create<CartPage>();
    }

    public async Task<InventoryPage> OpenInventory()
    {
        await InventoryLink.Click();
        return await Create<InventoryPage>();
    }

    public async Task<OrdersPage> OpenOrders()
    {
        await OrdersLink.Click();
        return await Create<OrdersPage>();
    }

    public async Task<ShopNavigation> SignOut()
    {
        await LogoutLink.Click();
        await LoginLink.Expect().ToBeVisibleAsync();
        return await Create<ShopNavigation>();
    }
}
