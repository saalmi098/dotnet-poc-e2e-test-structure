using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class CheckoutPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "checkout-form")]
    public required ILocator Form { get; init; }

    [Locator(LocatorKind.DataTestId, "checkout-name")]
    public required TextField Name { get; init; }

    [Locator(LocatorKind.DataTestId, "checkout-email")]
    public required TextField Email { get; init; }

    [Locator(LocatorKind.DataTestId, "checkout-confirm")]
    public required Button Confirm { get; init; }

    [Locator(LocatorKind.DataTestId, "checkout-order-number", Optional = true)]
    public required ILocator OrderNumber { get; init; }

    public async Task<string> PlaceOrder(string customerName, string email)
    {
        await Name.Fill(customerName);
        await Email.Fill(email);
        await Confirm.Click();
        await OrderNumber.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await OrderNumber.InnerTextAsync();
    }
}
