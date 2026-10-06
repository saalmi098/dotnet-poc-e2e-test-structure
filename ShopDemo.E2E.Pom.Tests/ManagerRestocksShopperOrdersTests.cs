using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ShopDemo.E2E.Pom.Tests.Pages;
using ShopDemo.E2E.Shared;
using Xunit;

namespace ShopDemo.E2E.Pom.Tests;

// Reuses for example LoginPage sign-in and CheckoutPage order placement across shopper and manager flows.
public sealed class ManagerRestocksShopperOrdersTests : PageTest
{
    private PomPages Pages // TODO: outsource to base class, same for ContextOptions
    {
        get
        {
            field ??= new PomPages(Page);
            return field;
        }
    }

    public override BrowserNewContextOptions ContextOptions()
        => new()
        {
            BaseURL = E2ETestSettings.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1365, Height = 900 },
            ColorScheme = ColorScheme.Light
        };

    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        await Pages.Login.SignInAsync(E2ETestSettings.StoreManager);
        await Pages.Inventory.OpenAsync();
        await Pages.Inventory.UpdateStockAsync(E2ETestSettings.CanvasToteId, 4);
        await Pages.Login.SignOutAsync();

        await Pages.Login.SignInAsync(E2ETestSettings.Shopper);
        await Pages.Catalog.OpenAsync();
        await Pages.Catalog.AddProductToCartAsync(E2ETestSettings.CanvasToteId);
        await Pages.Cart.ProceedToCheckoutAsync();
        var orderNumber = await Pages.Checkout.PlaceOrderAsync("Sample Shopper", E2ETestSettings.Shopper.Email);
        await Pages.Login.SignOutAsync();

        await Pages.Login.SignInAsync(E2ETestSettings.StoreManager);
        await Pages.Orders.OpenAsync();

        Assert.True(await Pages.Orders.ContainsOrderAsync(orderNumber));
    }

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        await Pages.Inventory.OpenDirectlyAsync();

        await Expect(Page.GetByTestId("login-form")).ToBeVisibleAsync();
        Assert.False(await Pages.Inventory.IsVisibleAsync());
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        await Pages.Login.SignInAsync(E2ETestSettings.Shopper);
        await Pages.Catalog.OpenAsync();
        await Pages.Catalog.AddProductToCartAsync("field-notebook");
        await Pages.Cart.ProceedToCheckoutAsync();
        var orderNumber = await Pages.Checkout.PlaceOrderAsync(
            "Sample Shopper",
            E2ETestSettings.Shopper.Email);

        await Pages.Orders.OpenAsync();

        Assert.True(await Pages.Orders.ContainsOrderAsync(orderNumber));
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        await Pages.Login.SignInAsync(E2ETestSettings.Shopper);

        Assert.False(await Pages.Inventory.IsManagementNavigationVisibleAsync());
    }
}
