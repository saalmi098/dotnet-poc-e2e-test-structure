using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class CatalogPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("catalog-page")]
    public ILocator Root { get; private set; } = null!;

    public async Task AddProductToCart(string productId)
    {
        var card = Root.GetByTestId($"product-card-{productId}");
        await card.GetByTestId($"product-add-{productId}").ClickAsync();
    }

    public async Task<ProductDetailsPage> OpenProductDetails(string productId)
    {
        var card = Root.GetByTestId($"product-card-{productId}");
        await card.GetByTestId($"product-details-{productId}").ClickAsync();
        return await Create<ProductDetailsPage>();
    }
}
