using Microsoft.Playwright;

namespace ShopDemo.E2E.Pom.Tests.Pages;

public sealed class PomPages(IPage page)
{
    public LoginPage Login { get; } = new(page);
    public CatalogPage Catalog { get; } = new(page);
    public InventoryPage Inventory { get; } = new(page);
    public CartPage Cart { get; } = new(page);
    public CheckoutPage Checkout { get; } = new(page);
    public OrdersPage Orders { get; } = new(page);
}
