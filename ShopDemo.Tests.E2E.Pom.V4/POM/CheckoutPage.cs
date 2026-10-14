using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class CheckoutPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("checkout-form")]
    public ILocator Form { get; private set; } = null!;

    [DataTestId("checkout-name")]
    public TextField Name { get; private set; } = null!;

    [DataTestId("checkout-email")]
    public TextField Email { get; private set; } = null!;

    [DataTestId("checkout-confirm")]
    public Button Confirm { get; private set; } = null!;

    [DataTestId("checkout-order-number", Required = false)]
    public ILocator OrderNumber { get; private set; } = null!;

    public async Task<string> PlaceOrder(string customerName, string email)
    {
        await Name.Fill(customerName);
        await Email.Fill(email);
        await Confirm.Click();
        await OrderNumber.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await OrderNumber.InnerTextAsync();
    }
}
