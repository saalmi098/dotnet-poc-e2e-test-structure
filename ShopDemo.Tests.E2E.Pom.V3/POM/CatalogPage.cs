using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class CatalogPage : PageComponent, IPageComponent<CatalogPage>
{
    private readonly IPage _page;

    public Required<ILocator> Root { get; }

    private CatalogPage(IPage page)
    {
        _page = page;
        Root = Require(page.GetByTestId("catalog-page"), $"{nameof(CatalogPage)}.{nameof(Root)}");
    }

    static CatalogPage IPageComponent<CatalogPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task AddProductToCart(string productId)
    {
        var card = Root.Value.GetByTestId($"product-card-{productId}");
        await card.GetByTestId($"product-add-{productId}").ClickAsync();
    }

    public async Task<ProductDetailsPage> OpenProductDetails(string productId)
    {
        var card = Root.Value.GetByTestId($"product-card-{productId}");
        await card.GetByTestId($"product-details-{productId}").ClickAsync();
        return await Create<ProductDetailsPage>(_page);
    }
}
