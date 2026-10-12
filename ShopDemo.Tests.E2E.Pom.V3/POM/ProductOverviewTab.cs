using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

/// <summary>
/// Created only by <see cref="ProductDetailsPage.SwitchToOverviewTab"/>, always scoped to the
/// visible overview panel passed as <c>baseLocator</c>.
/// </summary>
public sealed class ProductDetailsOverviewTab : PageComponent, IPageComponent<ProductDetailsOverviewTab>
{
    public Required<ILocator> Name { get; }
    public Required<ILocator> Description { get; }
    public Required<ILocator> Category { get; }
    public Required<ILocator> Price { get; }

    private ProductDetailsOverviewTab(ILocator panel)
    {
        Name = Require(panel.GetByTestId("product-overview-name"), $"{nameof(ProductDetailsOverviewTab)}.{nameof(Name)}");
        Description = Require(panel.GetByTestId("product-overview-description"), $"{nameof(ProductDetailsOverviewTab)}.{nameof(Description)}");
        Category = Require(panel.GetByTestId("product-overview-category"), $"{nameof(ProductDetailsOverviewTab)}.{nameof(Category)}");
        Price = Require(panel.GetByTestId("product-overview-price"), $"{nameof(ProductDetailsOverviewTab)}.{nameof(Price)}");
    }

    static ProductDetailsOverviewTab IPageComponent<ProductDetailsOverviewTab>.CreateInstance(IPage page, ILocator? baseLocator) =>
        new(baseLocator ?? throw new ArgumentNullException(nameof(baseLocator), $"{nameof(ProductDetailsOverviewTab)} requires a panel scope."));
}
