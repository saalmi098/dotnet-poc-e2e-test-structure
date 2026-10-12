using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class CheckoutPage : PageComponent, IPageComponent<CheckoutPage>
{
    public Required<ILocator> Form { get; }
    public Required<ILocator> Name { get; }
    public Required<ILocator> Email { get; }
    public Required<ILocator> Confirm { get; }
    public Optional<ILocator> OrderNumber { get; }

    private CheckoutPage(IPage page)
    {
        Form = Require(page.GetByTestId("checkout-form"), $"{nameof(CheckoutPage)}.{nameof(Form)}");
        Name = Require(page.GetByTestId("checkout-name"), $"{nameof(CheckoutPage)}.{nameof(Name)}");
        Email = Require(page.GetByTestId("checkout-email"), $"{nameof(CheckoutPage)}.{nameof(Email)}");
        Confirm = Require(page.GetByTestId("checkout-confirm"), $"{nameof(CheckoutPage)}.{nameof(Confirm)}");
        OrderNumber = Optional(page.GetByTestId("checkout-order-number"));
    }

    static CheckoutPage IPageComponent<CheckoutPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task<string> PlaceOrder(string customerName, string email)
    {
        await Name.Value.FillAsync(customerName);
        await Email.Value.FillAsync(email);
        await Confirm.Value.ClickAsync();
        await OrderNumber.Value.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await OrderNumber.Value.InnerTextAsync();
    }
}
