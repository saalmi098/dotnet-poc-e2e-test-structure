using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;
using ShopDemo.Tests.E2E.Pom.V2.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V2;

public sealed class PomJourneysTests : ShopDemoPageTest
{
    private readonly PageObjectFactory _factory = new();

    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        var navigation = await SignIn(E2ETestSettings.StoreManager);
        var inventory = await navigation.Bar.OpenInventory();
        await inventory.UpdateStock(E2ETestSettings.CanvasToteId, 4);

        navigation = await _factory.Create<ShopNavigation>(Page);
        navigation = await navigation.Bar.SignOut();
        var shopperLogin = await navigation.Bar.OpenLogin();
        navigation = await shopperLogin.SignIn(E2ETestSettings.Shopper);

        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart(E2ETestSettings.CanvasToteId);
        var cart = await (await _factory.Create<ShopNavigation>(Page)).Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        navigation = await _factory.Create<ShopNavigation>(Page);
        navigation = await navigation.Bar.SignOut();
        var managerLogin = await navigation.Bar.OpenLogin();
        navigation = await managerLogin.SignIn(E2ETestSettings.StoreManager);

        var orders = await navigation.Bar.OpenOrders();
        Assert.True(await orders.ContainsOrder(orderNumber));
    }

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        await Page.GotoAsync("/manage/inventory");

        var login = await _factory.Create<LoginPage>(Page);
        Assert.True(await login.Form.IsVisibleAsync());
        Assert.False(await Page.GetByTestId("inventory-page").IsVisibleAsync());
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);
        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart("field-notebook");

        var cart = await (await _factory.Create<ShopNavigation>(Page)).Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        navigation = await _factory.Create<ShopNavigation>(Page);
        var orders = await navigation.Bar.OpenOrders();
        Assert.True(await orders.ContainsOrder(orderNumber));
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);

        Assert.False(await navigation.Bar.InventoryLink.IsVisibleAsync());
    }

    [Fact]
    public async Task Product_details_tabs_bind_fields_after_parent_activates_them()
    {
        await Page.GotoAsync("/");
        var catalog = await _factory.Create<CatalogPage>(Page);
        var product = await catalog.OpenProductDetails(E2ETestSettings.CanvasToteId);

        Assert.Equal(
            "tab",
            await Page.GetByTestId("product-overview-tab")
                .EvaluateAsync<string>("element => element.closest('[role=tab]')?.getAttribute('role') ?? ''"));
        Assert.Equal(
            "tab",
            await Page.GetByTestId("product-inventory-tab")
                .EvaluateAsync<string>("element => element.closest('[role=tab]')?.getAttribute('role') ?? ''"));

        var overview = await product.SwitchToOverviewTab();
        Assert.Equal("Market Canvas Tote", await overview.Name.InnerTextAsync());
        Assert.False(string.IsNullOrWhiteSpace(await overview.Description.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Category.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Price.InnerTextAsync()));
        Assert.True(await overview.IsReady());

        var inventory = await product.SwitchToInventoryTab();
        Assert.Equal(E2ETestSettings.CanvasToteId, await inventory.ProductId.InnerTextAsync());
        Assert.Equal("0", await inventory.Stock.InnerTextAsync());
        Assert.Equal("Out of stock", await inventory.Availability.InnerTextAsync());
        Assert.True(await inventory.IsReady());
    }

    private async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Page.GotoAsync("/");
        var navigation = await _factory.Create<ShopNavigation>(Page);
        var login = await navigation.Bar.OpenLogin();
        return await login.SignIn(account);
    }
}
