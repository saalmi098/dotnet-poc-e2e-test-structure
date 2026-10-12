using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class ShopNavigation : PageComponent, IPageComponent<ShopNavigation>
{
    public NavigationBar Bar { get; }

    private ShopNavigation(IPage page)
    {
        Bar = RequireChild(new NavigationBar(page, page.Locator("css=.mud-appbar")));
    }

    static ShopNavigation IPageComponent<ShopNavigation>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);
}

/// <summary>
/// Nested component, never created independently — constructed directly by
/// <see cref="ShopNavigation"/>, so it has no <see cref="IPageComponent{TSelf}"/> factory method.
/// </summary>
public sealed class NavigationBar : PageComponent
{
    private readonly IPage _page;

    public Required<ILocator> CatalogLink { get; }
    public Required<ILocator> CartLink { get; }
    public Optional<ILocator> LoginLink { get; }
    public Optional<ILocator> InventoryLink { get; }
    public Optional<ILocator> OrdersLink { get; }
    public Optional<ILocator> LogoutLink { get; }

    internal NavigationBar(IPage page, ILocator bar)
    {
        _page = page;
        Require(bar, $"{nameof(NavigationBar)} app bar container");

        CatalogLink = Require(bar.GetByTestId("nav-catalog-desktop"), $"{nameof(NavigationBar)}.{nameof(CatalogLink)}");
        CartLink = Require(bar.GetByTestId("nav-cart-desktop"), $"{nameof(NavigationBar)}.{nameof(CartLink)}");
        LoginLink = Optional(bar.GetByTestId("nav-login-desktop"));
        InventoryLink = Optional(bar.GetByTestId("nav-inventory-desktop"));
        OrdersLink = Optional(bar.GetByTestId("nav-orders-desktop"));
        LogoutLink = Optional(bar.GetByTestId("nav-logout-desktop"));
    }

    public async Task<LoginPage> OpenLogin()
    {
        await LoginLink.Value.ClickAsync();
        return await Create<LoginPage>(_page);
    }

    public async Task<CatalogPage> OpenCatalog()
    {
        await CatalogLink.Value.ClickAsync();
        return await Create<CatalogPage>(_page);
    }

    public async Task<CartPage> OpenCart()
    {
        await CartLink.Value.ClickAsync();
        return await Create<CartPage>(_page);
    }

    public async Task<InventoryPage> OpenInventory()
    {
        await InventoryLink.Value.ClickAsync();
        return await Create<InventoryPage>(_page);
    }

    public async Task<OrdersPage> OpenOrders()
    {
        await OrdersLink.Value.ClickAsync();
        return await Create<OrdersPage>(_page);
    }

    public async Task<ShopNavigation> SignOut()
    {
        await LogoutLink.Value.ClickAsync();
        await LoginLink.Value.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await Create<ShopNavigation>(_page);
    }
}
