using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Pom.V4.POM;
using ShopDemo.Tests.E2E.Shared;
using System.Diagnostics;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class PomJourneysTests(ITestOutputHelper output) : ShopDemoPageTest
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
    public async Task Catalog_can_be_filtered_by_category_through_the_select_control()
    {
        await Page.GotoAsync("/");
        var catalog = await _factory.Create<CatalogPage>(Page);
        await catalog.ProductCards.First.WaitForAsync();
        var allProducts = await catalog.ProductCards.CountAsync();

        await catalog.FilterByCategory("Home");

        Assert.Equal("Home", await catalog.Category.GetSelectedText());
        await catalog.Category.Expect().ToContainTextAsync("Home");
        var homeProducts = await catalog.ProductCards.CountAsync();
        Assert.InRange(homeProducts, 1, allProducts - 1);

        await catalog.FilterByCategory("All categories");

        await catalog.Category.Expect().ToContainTextAsync("All categories");
        await Assertions.Expect(catalog.ProductCards).ToHaveCountAsync(allProducts);
    }

    [Fact]
    public async Task Catalog_can_be_searched_through_the_text_field_control()
    {
        await Page.GotoAsync("/");
        var catalog = await _factory.Create<CatalogPage>(Page);
        await catalog.ProductCards.First.WaitForAsync();

        await catalog.SearchFor("Tote");

        await Expect(catalog.GetProductCard(E2ETestSettings.CanvasToteId)).ToBeVisibleAsync();
        Assert.Equal("Tote", await catalog.Search.GetValue());
        await Expect(catalog.ProductCards).ToHaveCountAsync(1);

        await catalog.SearchFor("no product has this name");

        await Expect(catalog.ProductCards).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Login_page_controls_are_bound_and_ready()
    {
        await Page.GotoAsync("/login");
        var login = await _factory.Create<LoginPage>(Page);

        Assert.True(await login.IsReady());
        await login.Email.Fill(E2ETestSettings.Shopper.Email);
        Assert.Equal(E2ETestSettings.Shopper.Email, await login.Email.GetValue());
        await login.Password.Expect().ToBeVisibleAsync();
        await login.Submit.Expect().ToContainTextAsync("Log in", new() { IgnoreCase = true });
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
        await navigation.Bar.InventoryLink.Expect().Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Product_details_tabs_bind_fields_after_parent_activates_them()
    {
        await Page.GotoAsync("/");
        var catalog = await _factory.Create<CatalogPage>(Page);
        var product = await catalog.OpenProductDetails(E2ETestSettings.CanvasToteId);

        // the Tab control resolves its root to the enclosing role=tab element of the labelled span
        await product.OverviewTab.Expect().ToHaveAttributeAsync("role", "tab");
        await product.InventoryTab.Expect().ToHaveAttributeAsync("role", "tab");

        var overview = await product.SwitchToOverviewTab();
        Assert.Equal("Market Canvas Tote", await overview.Name.InnerTextAsync());
        Assert.False(string.IsNullOrWhiteSpace(await overview.Description.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Category.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Price.InnerTextAsync()));
        Assert.True(await overview.IsReady());

        Log("Starting Inventory tab activation and page-object binding.");

        var inventoryLoad = Stopwatch.StartNew();
        var inventoryTask = product.SwitchToInventoryTab();
        await Page.GetByTestId("product-inventory-loading")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        Log($"Loading indicator is visible after {inventoryLoad.Elapsed}; inventory page-object task completed: {inventoryTask.IsCompleted}.");

        var inventory = await inventoryTask;
        Log($"Inventory page object is ready after {inventoryLoad.Elapsed}.");

        Assert.True(
            inventoryLoad.Elapsed >= TimeSpan.FromSeconds(4.5),
            $"Inventory loaded too quickly: {inventoryLoad.Elapsed}.");

        await Expect(inventory.ProductId).ToHaveTextAsync(E2ETestSettings.CanvasToteId);
        await Expect(inventory.Stock).ToHaveTextAsync("0");
        await Expect(inventory.Availability).ToHaveTextAsync("Out of stock");
        await Expect(inventory.Rows).ToHaveCountAsync(10);
        await inventory.Pager.Expect().ToBeVisibleAsync();
        Assert.True(await inventory.IsReady());
    }

    [Fact]
    public async Task Product_inventory_grid_can_be_read_and_paged_through_grid_controls()
    {
        await Page.GotoAsync("/");
        var catalog = await _factory.Create<CatalogPage>(Page);
        var product = await catalog.OpenProductDetails(E2ETestSettings.CanvasToteId);
        await product.SwitchToOverviewTab();
        await product.OverviewTab.Expect().ToHaveAttributeAsync("aria-selected", "true");

        var inventory = await product.SwitchToInventoryTab();
        await product.InventoryTab.Expect().ToHaveAttributeAsync("aria-selected", "true");
        await product.OverviewTab.Expect().ToHaveAttributeAsync("aria-selected", "false");

        await Expect(inventory.ColumnHeaders).ToHaveTextAsync(["#", "Product ID", "Stock", "Availability"]);
        await Expect(inventory.Rows).ToHaveCountAsync(10);
        await Expect(inventory.FirstRowProductId).ToHaveTextAsync(E2ETestSettings.CanvasToteId);
        await Expect(inventory.PageRange).ToHaveTextAsync("1-10 of 25");

        await inventory.GoToNextPage();
        await Expect(inventory.PageRange).ToHaveTextAsync("11-20 of 25");
        await Expect(inventory.Rows).ToHaveCountAsync(10);

        await inventory.GoToLastPage();
        await Expect(inventory.PageRange).ToHaveTextAsync("21-25 of 25");
        await Expect(inventory.Rows).ToHaveCountAsync(5);

        await inventory.GoToFirstPage();
        await inventory.ShowRowsPerPage(25);
        await Expect(inventory.PageRange).ToHaveTextAsync("1-25 of 25");
        await Expect(inventory.ProductIdCells).ToHaveTextAsync(Enumerable.Repeat(E2ETestSettings.CanvasToteId, 25).ToArray());
    }

    private async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Page.GotoAsync("/");
        var navigation = await _factory.Create<ShopNavigation>(Page);
        var login = await navigation.Bar.OpenLogin();
        return await login.SignIn(account);
    }

    private void Log(string message)
    {
        E2ETrace.Write(typeof(PomJourneysTests), message, output.WriteLine);
    }
}
