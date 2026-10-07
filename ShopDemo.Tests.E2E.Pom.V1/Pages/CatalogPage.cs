using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V1.Pages;

public sealed class CatalogPage(IPage page)
{
    public Task OpenAsync() => page.GetByTestId("nav-catalog-desktop").ClickAsync();

    public async Task AddProductToCartAsync(string productId)
    {
        var product = page.GetByTestId($"product-card-{productId}");
        await product.GetByTestId($"product-add-{productId}").ClickAsync();
    }

    public Task<bool> IsVisibleAsync() => page.GetByTestId("catalog-page").IsVisibleAsync();
}
