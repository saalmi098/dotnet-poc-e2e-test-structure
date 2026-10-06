using Microsoft.Playwright;

namespace ShopDemo.E2E.Pom.Tests.Pages;

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
