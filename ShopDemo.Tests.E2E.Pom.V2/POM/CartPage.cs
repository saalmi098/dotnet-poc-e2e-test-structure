using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class CartPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("cart-page")]
    public ILocator Root { get; private set; } = null!;

    public async Task<CheckoutPage> ProceedToCheckout()
    {
        await Root.GetByTestId("cart-checkout").ClickAsync();
        return await Create<CheckoutPage>();
    }
}
