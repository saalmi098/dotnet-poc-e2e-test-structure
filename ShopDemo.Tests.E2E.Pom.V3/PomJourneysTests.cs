using System.Diagnostics;
using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;
using ShopDemo.Tests.E2E.Pom.V3.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V3;

public sealed class PomJourneysTests : ShopDemoPageTest
{
    private readonly ITestOutputHelper _output;

    public PomJourneysTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        var navigation = await SignIn(E2ETestSettings.StoreManager);
        var inventory = await navigation.Bar.OpenInventory();
        await inventory.UpdateStock(E2ETestSettings.CanvasToteId, 4);

        navigation = await PageComponent.Create<ShopNavigation>(Page);
        navigation = await navigation.Bar.SignOut();
        var shopperLogin = await navigation.Bar.OpenLogin();
        navigation = await shopperLogin.SignIn(E2ETestSettings.Shopper);

        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart(E2ETestSettings.CanvasToteId);
        var cart = await (await PageComponent.Create<ShopNavigation>(Page)).Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        navigation = await PageComponent.Create<ShopNavigation>(Page);
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

        var login = await PageComponent.Create<LoginPage>(Page);
        Assert.True(await login.Form.Value.IsVisibleAsync());
        Assert.False(await Page.GetByTestId("inventory-page").IsVisibleAsync());
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);
        var catalog = await navigation.Bar.OpenCatalog();
        await catalog.AddProductToCart("field-notebook");

        var cart = await (await PageComponent.Create<ShopNavigation>(Page)).Bar.OpenCart();
        var checkout = await cart.ProceedToCheckout();
        var orderNumber = await checkout.PlaceOrder("Sample Shopper", E2ETestSettings.Shopper.Email);

        navigation = await PageComponent.Create<ShopNavigation>(Page);
        var orders = await navigation.Bar.OpenOrders();
        Assert.True(await orders.ContainsOrder(orderNumber));
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        var navigation = await SignIn(E2ETestSettings.Shopper);

        Assert.False(await navigation.Bar.InventoryLink.Value.IsVisibleAsync());
    }

    [Fact]
    public async Task Product_details_tabs_bind_fields_after_parent_activates_them()
    {
        await Page.GotoAsync("/");
        var catalog = await PageComponent.Create<CatalogPage>(Page);
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
        Assert.Equal("Market Canvas Tote", await overview.Name.Value.InnerTextAsync());
        Assert.False(string.IsNullOrWhiteSpace(await overview.Description.Value.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Category.Value.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Price.Value.InnerTextAsync()));
        Assert.True(await overview.IsReady());

        var inventoryLoad = Stopwatch.StartNew();
        Log("Starting Inventory tab activation and page-object binding.");
        var inventoryTask = product.SwitchToInventoryTab();
        await Page.GetByTestId("product-inventory-loading")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Log(
            $"Loading indicator is visible after {inventoryLoad.Elapsed}; inventory page-object task completed: {inventoryTask.IsCompleted}.");
        var inventory = await inventoryTask;
        Log($"Inventory page object is ready after {inventoryLoad.Elapsed}.");
        Assert.True(
            inventoryLoad.Elapsed >= TimeSpan.FromSeconds(4.5),
            $"Inventory loaded too quickly: {inventoryLoad.Elapsed}.");

        Assert.Equal(E2ETestSettings.CanvasToteId, await inventory.ProductId.Value.InnerTextAsync());
        Assert.Equal("0", await inventory.Stock.Value.InnerTextAsync());
        Assert.Equal("Out of stock", await inventory.Availability.Value.InnerTextAsync());
        Assert.Equal(10, await inventory.Grid.Value.Locator("[data-testid^='product-inventory-row-']").CountAsync());
        Assert.True(await inventory.Pager.Value.IsVisibleAsync());
        Assert.True(await inventory.IsReady());
    }

    private async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Page.GotoAsync("/");
        var navigation = await PageComponent.Create<ShopNavigation>(Page);
        var login = await navigation.Bar.OpenLogin();
        return await login.SignIn(account);
    }

    private void Log(string message)
    {
        E2ETrace.Write(typeof(PomJourneysTests), message, _output.WriteLine);
    }
}
