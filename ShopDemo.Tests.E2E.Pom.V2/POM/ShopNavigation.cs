using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class ShopNavigation(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.Css, ".mud-appbar")]
    public NavigationBar Bar { get; private set; } = null!;
}

public sealed class NavigationBar(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("nav-catalog-desktop")]
    public ILocator CatalogLink { get; private set; } = null!;

    [DataTestId("nav-cart-desktop")]
    public ILocator CartLink { get; private set; } = null!;

    [DataTestId("nav-login-desktop", Required = false)]
    public ILocator LoginLink { get; private set; } = null!;

    [DataTestId("nav-inventory-desktop", Required = false)]
    public ILocator InventoryLink { get; private set; } = null!;

    [DataTestId("nav-orders-desktop", Required = false)]
    public ILocator OrdersLink { get; private set; } = null!;

    [DataTestId("nav-logout-desktop", Required = false)]
    public ILocator LogoutLink { get; private set; } = null!;

    public async Task<LoginPage> OpenLogin()
    {
        await LoginLink.ClickAsync();
        return await Create<LoginPage>();
    }

    public async Task<CatalogPage> OpenCatalog()
    {
        await CatalogLink.ClickAsync();
        return await Create<CatalogPage>();
    }

    public async Task<CartPage> OpenCart()
    {
        await CartLink.ClickAsync();
        return await Create<CartPage>();
    }

    public async Task<InventoryPage> OpenInventory()
    {
        await InventoryLink.ClickAsync();
        return await Create<InventoryPage>();
    }

    public async Task<OrdersPage> OpenOrders()
    {
        await OrdersLink.ClickAsync();
        return await Create<OrdersPage>();
    }

    public async Task<ShopNavigation> SignOut()
    {
        await LogoutLink.ClickAsync();
        await LoginLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await Create<ShopNavigation>();
    }
}
