using ShopDemo.E2E.Shared;
using Xunit;

namespace ShopDemo.E2E.Pom.Tests;

public sealed class ManagerRestocksShopperOrdersTests(PomPagesFixture pomPagesFixture)
    : ShopDemoPomPageTest(pomPagesFixture)
{
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
}
