using ShopDemo.E2E.Pom.Tests.Pages;
using ShopDemo.E2E.Shared;
using Xunit;

namespace ShopDemo.E2E.Pom.Tests;

public sealed class ManagerRestocksShopperOrdersTests : ShopDemoPageTest
{
    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        var login = new LoginPage(Page);
        var inventory = new InventoryPage(Page);
        var catalog = new CatalogPage(Page);
        var cart = new CartPage(Page);
        var checkout = new CheckoutPage(Page);
        var orders = new OrdersPage(Page);

        await login.SignInAsync(E2ETestSettings.StoreManager);
        await inventory.OpenAsync();
        await inventory.UpdateStockAsync(E2ETestSettings.CanvasToteId, 4);
        await login.SignOutAsync();

        await login.SignInAsync(E2ETestSettings.Shopper);
        await catalog.OpenAsync();
        await catalog.AddProductToCartAsync(E2ETestSettings.CanvasToteId);
        await cart.ProceedToCheckoutAsync();
        var orderNumber = await checkout.PlaceOrderAsync("Sample Shopper", E2ETestSettings.Shopper.Email);
        await login.SignOutAsync();

        await login.SignInAsync(E2ETestSettings.StoreManager);
        await orders.OpenAsync();

        Assert.True(await orders.ContainsOrderAsync(orderNumber));
    }

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        var inventory = new InventoryPage(Page);

        await Page.GotoAsync("/manage/inventory");

        await Expect(Page.GetByTestId("login-form")).ToBeVisibleAsync();
        Assert.False(await inventory.IsVisibleAsync());
    }
}
