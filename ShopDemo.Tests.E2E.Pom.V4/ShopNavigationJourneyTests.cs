using ShopDemo.Tests.E2E.Pom.V4.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class ShopNavigationJourneyTests : ShopDemoPageTestV4<ShopNavigation>
{
    protected override string PageUrl => "/";

    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        var navigation = await SignIn(E2ETestSettings.StoreManager);
        var inventory = await navigation.Bar.OpenInventory();
        await inventory.UpdateStock(E2ETestSettings.CanvasToteId, 4);

        navigation = await navigation.Bar.SignOut();
        var shopperLogin = await navigation.Bar.OpenLogin();
        navigation = await shopperLogin.SignIn(E2ETestSettings.Shopper);

        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart(E2ETestSettings.CanvasToteId);
        var cart = await navigation.Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        navigation = await navigation.Bar.SignOut();
        var managerLogin = await navigation.Bar.OpenLogin();
        navigation = await managerLogin.SignIn(E2ETestSettings.StoreManager);

        var orders = await navigation.Bar.OpenOrders();
        Assert.True(await orders.ContainsOrder(orderNumber));
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);
        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart("field-notebook");

        var cart = await navigation.Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        var orders = await navigation.Bar.OpenOrders();
        Assert.True(await orders.ContainsOrder(orderNumber));
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);
        await navigation.Bar.InventoryLink.Expect().Not.ToBeVisibleAsync();
    }

    private async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        var login = await Model.Bar.OpenLogin();
        return await login.SignIn(account);
    }
}
