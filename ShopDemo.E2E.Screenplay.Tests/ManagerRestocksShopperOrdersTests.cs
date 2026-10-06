using ShopDemo.E2E.Screenplay.Tests.Screenplay;
using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;
using ShopDemo.E2E.Screenplay.Tests.Screenplay.Questions;
using ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;
using ShopDemo.E2E.Shared;
using Xunit;

namespace ShopDemo.E2E.Screenplay.Tests;

// Reuses for example SignIn, AddProductToCart, and CompleteCheckout across shopper and manager scenarios.
public sealed class ManagerRestocksShopperOrdersTests : ShopDemoPageTest
{
    [Fact]
    public async Task Manager_can_restock_then_review_shopper_order()
    {
        var browseTheWeb = new BrowseTheWeb(Page);
        var manager = new Actor("Store manager").Can(browseTheWeb);
        var shopper = new Actor("Shopper").Can(browseTheWeb);

        await manager.AttemptsToAsync(
            new SignIn(E2ETestSettings.StoreManager),
            new RestockProduct(E2ETestSettings.CanvasToteId, 4),
            new SignOut());

        await shopper.AttemptsToAsync(
            new SignIn(E2ETestSettings.Shopper),
            new AddProductToCart(E2ETestSettings.CanvasToteId),
            new CompleteCheckout("Sample Shopper", E2ETestSettings.Shopper.Email));

        var orderNumber = await shopper.AsksAsync(new ConfirmedOrderNumber());

        await shopper.AttemptsToAsync(new SignOut());

        await manager.AttemptsToAsync(
            new SignIn(E2ETestSettings.StoreManager),
            new OpenOrderHistory());

        Assert.True(await manager.AsksAsync(new OrderIsVisible(orderNumber)));
    }

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        var guest = new Actor("Guest").Can(new BrowseTheWeb(Page));

        await guest.AttemptsToAsync(new OpenInventory());

        await Expect(Page.GetByTestId("login-form")).ToBeVisibleAsync();
        Assert.False(await guest.AsksAsync(new InventoryIsVisible()));
    }

    [Fact]
    public async Task Shopper_can_view_their_own_order_history()
    {
        var shopper = new Actor("Shopper").Can(new BrowseTheWeb(Page));

        await shopper.AttemptsToAsync(
            new SignIn(E2ETestSettings.Shopper),
            new AddProductToCart("field-notebook"),
            new CompleteCheckout("Sample Shopper", E2ETestSettings.Shopper.Email));

        var orderNumber = await shopper.AsksAsync(new ConfirmedOrderNumber());

        await shopper.AttemptsToAsync(new OpenOrderHistory());

        Assert.True(await shopper.AsksAsync(new OrderIsVisible(orderNumber)));
    }

    [Fact]
    public async Task Shopper_does_not_see_inventory_management_navigation()
    {
        var shopper = new Actor("Shopper").Can(new BrowseTheWeb(Page));

        await shopper.AttemptsToAsync(new SignIn(E2ETestSettings.Shopper));

        Assert.False(await shopper.AsksAsync(new InventoryNavigationIsVisible()));
    }
}
