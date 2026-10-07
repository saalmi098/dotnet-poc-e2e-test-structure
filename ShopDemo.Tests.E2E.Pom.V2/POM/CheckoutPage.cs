using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class CheckoutPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("checkout-form")]
    public ILocator Form { get; private set; } = null!;

    [DataTestId("checkout-name")]
    public ILocator Name { get; private set; } = null!;

    [DataTestId("checkout-email")]
    public ILocator Email { get; private set; } = null!;

    [DataTestId("checkout-confirm")]
    public ILocator Confirm { get; private set; } = null!;

    [DataTestId("checkout-order-number", Required = false)]
    public ILocator OrderNumber { get; private set; } = null!;

    public async Task<string> PlaceOrder(string customerName, string email)
    {
        await Name.FillAsync(customerName);
        await Email.FillAsync(email);
        await Confirm.ClickAsync();
        await OrderNumber.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await OrderNumber.InnerTextAsync();
    }
}
