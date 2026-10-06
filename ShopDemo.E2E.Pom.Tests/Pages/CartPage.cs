using Microsoft.Playwright;

namespace ShopDemo.E2E.Pom.Tests.Pages;

public sealed class CartPage(IPage page)
{
    public async Task ProceedToCheckoutAsync()
    {
        await page.GetByTestId("nav-cart-desktop").ClickAsync();
        await page.GetByTestId("cart-checkout").ClickAsync();
    }
}
