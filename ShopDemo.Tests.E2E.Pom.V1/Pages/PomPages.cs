using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V1.Pages;

public sealed class PomPages(IPage page)
{
    public LoginPage Login { get; } = new(page);
    public CatalogPage Catalog { get; } = new(page);
    public InventoryPage Inventory { get; } = new(page);
    public CartPage Cart { get; } = new(page);
    public CheckoutPage Checkout { get; } = new(page);
    public OrdersPage Orders { get; } = new(page);
}
