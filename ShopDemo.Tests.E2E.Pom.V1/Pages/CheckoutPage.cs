using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V1.Pages;

public sealed class CheckoutPage(IPage page)
{
    public async Task<string> PlaceOrderAsync(string name, string email)
    {
        await page.GetByTestId("checkout-name").FillAsync(name);
        await page.GetByTestId("checkout-email").FillAsync(email);
        await page.GetByTestId("checkout-confirm").ClickAsync();

        var orderNumber = page.GetByTestId("checkout-order-number");
        await orderNumber.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await orderNumber.InnerTextAsync();
    }
}
