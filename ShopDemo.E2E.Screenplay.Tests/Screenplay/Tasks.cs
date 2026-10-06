using ShopDemo.E2E.Shared;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay;

public sealed record SignIn(DemoAccount Account) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        var loginLink = page.GetByTestId("nav-login-desktop");
        if (await loginLink.CountAsync() == 0)
        {
            await page.GotoAsync("/");
            await loginLink.WaitForAsync();
        }

        await loginLink.ClickAsync();
        await page.GetByTestId("login-email").FillAsync(Account.Email);
        await page.GetByTestId("login-password").FillAsync(Account.Password);
        await page.GetByTestId("login-submit").ClickAsync();
        await page.GetByTestId("nav-logout-desktop").WaitForAsync();
    }
}

public sealed record SignOut : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-logout-desktop").ClickAsync();
        await page.GetByTestId("nav-login-desktop").WaitForAsync();
    }
}

public sealed record RestockProduct(string ProductId, int Quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-inventory-desktop").ClickAsync();
        await page.GetByTestId("inventory-page").WaitForAsync();
        var item = page.GetByTestId($"inventory-item-{ProductId}");
        await item.GetByTestId($"inventory-stock-{ProductId}").Locator("input").FillAsync(Quantity.ToString());
        await item.GetByTestId($"inventory-save-{ProductId}").ClickAsync();
    }
}

public sealed record AddProductToCart(string ProductId) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-catalog-desktop").ClickAsync();
        await page.GetByTestId($"product-card-{ProductId}")
            .GetByTestId($"product-add-{ProductId}")
            .ClickAsync();
    }
}

public sealed record CompleteCheckout(string CustomerName, string Email) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-cart-desktop").ClickAsync();
        await page.GetByTestId("cart-checkout").ClickAsync();
        await page.GetByTestId("checkout-name").FillAsync(CustomerName);
        await page.GetByTestId("checkout-email").FillAsync(Email);
        await page.GetByTestId("checkout-confirm").ClickAsync();
        await page.GetByTestId("checkout-order-number")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
    }
}

public sealed record OpenOrderHistory : ITask
{
    public Task PerformAsAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("nav-orders-desktop").ClickAsync();
}

public sealed record OpenInventory : ITask
{
    public Task PerformAsAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GotoAsync("/manage/inventory");
}
