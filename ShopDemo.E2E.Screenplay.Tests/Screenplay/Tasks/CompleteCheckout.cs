using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;

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
