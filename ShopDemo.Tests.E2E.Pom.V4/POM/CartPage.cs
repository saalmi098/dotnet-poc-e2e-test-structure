using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class CartPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "cart-page")]
    public ILocator Root { get; private set; } = null!;

    // Not shown to a store manager (who cannot check out), so it must not block readiness.
    [Locator(LocatorKind.DataTestId, "cart-checkout", Required = false)]
    public Button Checkout { get; private set; } = null!;

    public async Task<CheckoutPage> ProceedToCheckout()
    {
        await Checkout.Click();
        return await Create<CheckoutPage>();
    }
}
