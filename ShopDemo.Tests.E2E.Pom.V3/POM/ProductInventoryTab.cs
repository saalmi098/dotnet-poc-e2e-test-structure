using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

/// <summary>
/// Created only by <see cref="ProductDetailsPage.SwitchToInventoryTab"/>, always scoped to the
/// visible inventory panel passed as <c>baseLocator</c>.
/// </summary>
public sealed class ProductDetailsInventoryTab : PageComponent, IPageComponent<ProductDetailsInventoryTab>
{
    public Required<ILocator> ProductId { get; }
    public Required<ILocator> Stock { get; }
    public Required<ILocator> Availability { get; }

    private ProductDetailsInventoryTab(ILocator panel)
    {
        ProductId = Require(panel.GetByTestId("product-inventory-id"), $"{nameof(ProductDetailsInventoryTab)}.{nameof(ProductId)}");
        Stock = Require(panel.GetByTestId("product-inventory-stock"), $"{nameof(ProductDetailsInventoryTab)}.{nameof(Stock)}");
        Availability = Require(panel.GetByTestId("product-inventory-availability"), $"{nameof(ProductDetailsInventoryTab)}.{nameof(Availability)}");
    }

    static ProductDetailsInventoryTab IPageComponent<ProductDetailsInventoryTab>.CreateInstance(IPage page, ILocator? baseLocator) =>
        new(baseLocator ?? throw new ArgumentNullException(nameof(baseLocator), $"{nameof(ProductDetailsInventoryTab)} requires a panel scope."));
}
