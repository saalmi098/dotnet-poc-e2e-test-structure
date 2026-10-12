using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class CartPage : PageComponent, IPageComponent<CartPage>
{
    private readonly IPage _page;

    public Required<ILocator> Root { get; }

    private CartPage(IPage page)
    {
        _page = page;
        Root = Require(page.GetByTestId("cart-page"), $"{nameof(CartPage)}.{nameof(Root)}");
    }

    static CartPage IPageComponent<CartPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task<CheckoutPage> ProceedToCheckout()
    {
        await Root.Value.GetByTestId("cart-checkout").ClickAsync();
        return await Create<CheckoutPage>(_page);
    }
}
