using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.POM;

public sealed class ProductDetailsInventoryTab(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("product-inventory-id")]
    public ILocator ProductId { get; private set; } = null!;

    [DataTestId("product-inventory-stock")]
    public ILocator Stock { get; private set; } = null!;

    [DataTestId("product-inventory-availability")]
    public ILocator Availability { get; private set; } = null!;
}
