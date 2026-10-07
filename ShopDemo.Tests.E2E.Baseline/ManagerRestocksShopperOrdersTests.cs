using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Baseline;

// Uses Playwright directly: selectors, navigation, and UI actions are repeated in each test.
public sealed class ManagerRestocksShopperOrdersTests : ShopDemoPageTest
{
    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        await Page.GotoAsync("/");

        await Page.GetByTestId("nav-login-desktop").ClickAsync();
        await Page.GetByTestId("login-email").FillAsync(E2ETestSettings.StoreManager.Email);
        await Page.GetByTestId("login-password").FillAsync(E2ETestSettings.StoreManager.Password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.GetByTestId("nav-logout-desktop").WaitForAsync();

        await Page.GetByTestId("nav-inventory-desktop").ClickAsync();
        await Page.GetByTestId("inventory-page").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var inventoryItem = Page.GetByTestId($"inventory-item-{E2ETestSettings.CanvasToteId}");
        await inventoryItem.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await inventoryItem.GetByTestId($"inventory-stock-{E2ETestSettings.CanvasToteId}")
            .Locator("input")
            .FillAsync("4");
        await inventoryItem.GetByTestId($"inventory-save-{E2ETestSettings.CanvasToteId}").ClickAsync();
        await inventoryItem.GetByTestId($"inventory-current-stock-{E2ETestSettings.CanvasToteId}")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await Page.GetByTestId("nav-logout-desktop").ClickAsync();
        await Page.GetByTestId("nav-login-desktop").WaitForAsync();
        await Page.GetByTestId("nav-login-desktop").ClickAsync();
        await Page.GetByTestId("login-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("login-password").FillAsync(E2ETestSettings.Shopper.Password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.GetByTestId("nav-logout-desktop").WaitForAsync();

        await Page.GetByTestId("nav-catalog-desktop").ClickAsync();
        await Page.GetByTestId("catalog-page").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var product = Page.GetByTestId($"product-card-{E2ETestSettings.CanvasToteId}");
        await product.GetByTestId($"product-add-{E2ETestSettings.CanvasToteId}").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();
        await Page.GetByTestId("checkout-name").FillAsync("Sample Shopper");
        await Page.GetByTestId("checkout-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("checkout-confirm").ClickAsync();
        var orderNumberLocator = Page.GetByTestId("checkout-order-number");
        await orderNumberLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var orderNumber = await orderNumberLocator.InnerTextAsync();

        await Page.GetByTestId("nav-logout-desktop").ClickAsync();
        await Page.GetByTestId("nav-login-desktop").WaitForAsync();
        await Page.GetByTestId("nav-login-desktop").ClickAsync();
        await Page.GetByTestId("login-email").FillAsync(E2ETestSettings.StoreManager.Email);
        await Page.GetByTestId("login-password").FillAsync(E2ETestSettings.StoreManager.Password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.GetByTestId("nav-logout-desktop").WaitForAsync();
        await Page.GetByTestId("nav-orders-desktop").ClickAsync();

        Assert.True(await Page.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0);
    }

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        await Page.GotoAsync("/manage/inventory");

        await Expect(Page.GetByTestId("login-form")).ToBeVisibleAsync();
        Assert.False(await Page.GetByTestId("inventory-page").IsVisibleAsync());
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        await Page.GotoAsync("/");

        await Page.GetByTestId("nav-login-desktop").ClickAsync();
        await Page.GetByTestId("login-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("login-password").FillAsync(E2ETestSettings.Shopper.Password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.GetByTestId("nav-logout-desktop").WaitForAsync();

        await Page.GetByTestId("nav-catalog-desktop").ClickAsync();
        await Page.GetByTestId("catalog-page").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var product = Page.GetByTestId("product-card-field-notebook");
        await product.GetByTestId("product-add-field-notebook").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();
        await Page.GetByTestId("checkout-name").FillAsync("Sample Shopper");
        await Page.GetByTestId("checkout-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("checkout-confirm").ClickAsync();
        var orderNumberLocator = Page.GetByTestId("checkout-order-number");
        await orderNumberLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var orderNumber = await orderNumberLocator.InnerTextAsync();

        await Page.GetByTestId("nav-orders-desktop").ClickAsync();

        Assert.True(await Page.GetByTestId($"order-card-{orderNumber}").CountAsync() > 0);
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        await Page.GotoAsync("/");

        await Page.GetByTestId("nav-login-desktop").ClickAsync();
        await Page.GetByTestId("login-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("login-password").FillAsync(E2ETestSettings.Shopper.Password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.GetByTestId("nav-logout-desktop").WaitForAsync();

        Assert.False(await Page.GetByTestId("nav-inventory-desktop").IsVisibleAsync());
    }
}
