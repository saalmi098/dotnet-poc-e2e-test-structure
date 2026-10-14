using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class CatalogPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("catalog-page")]
    public ILocator Root { get; private set; } = null!;

    [DataTestId("catalog-search")]
    public TextField Search { get; private set; } = null!;

    [DataTestId("catalog-category")]
    public Select Category { get; private set; } = null!;

    public Task SearchFor(string text) => Search.Fill(text);

    public Task FilterByCategory(string category) => Category.SelectOption(category);

    public ILocator GetProductCard(string productId) => Root.GetByTestId($"product-card-{productId}");

    public ILocator ProductCards => Root.Locator("[data-testid^='product-card-']");

    public async Task AddProductToCart(string productId)
    {
        // The test ids contain the product id, so these buttons cannot be attribute-bound; they are created directly.
        var addButton = new Button(Page, GetProductCard(productId).GetByTestId($"product-add-{productId}"));
        await addButton.Click();
    }

    public async Task<ProductDetailsPage> OpenProductDetails(string productId)
    {
        var detailsButton = new Button(Page, GetProductCard(productId).GetByTestId($"product-details-{productId}"));
        await detailsButton.Click();
        return await Create<ProductDetailsPage>();
    }
}
