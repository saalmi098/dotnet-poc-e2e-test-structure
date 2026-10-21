using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class CartPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "cart-page")]
    public required ILocator Root { get; init; }

    // Not shown to a store manager (who cannot check out), so it must not block readiness.
    [Locator(LocatorKind.DataTestId, "cart-checkout", Optional = true)]
    public required Button Checkout { get; init; }

    public async Task<CheckoutPage> ProceedToCheckout()
    {
        await Checkout.Click();
        return await CreatePageObject<CheckoutPage>();
    }
}
