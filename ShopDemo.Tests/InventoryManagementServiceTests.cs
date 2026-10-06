using ShopDemo.Application;
using ShopDemo.Application.Interfaces;
using ShopDemo.Domain;
using Xunit;

namespace ShopDemo.Tests;

public sealed class InventoryManagementServiceTests
{
    [Fact]
    public async Task Store_manager_can_update_product_stock()
    {
        var catalog = new FakeInventoryCatalog();
        var service = new InventoryManagementService(
            new FakeAuthService(new DemoUser("admin@test.com", "Manager", DemoRole.StoreManager)),
            catalog);

        var product = await service.UpdateStockAsync("canvas-tote", 5, TestContext.Current.CancellationToken);

        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task Shopper_cannot_update_product_stock()
    {
        var catalog = new FakeInventoryCatalog();
        var service = new InventoryManagementService(
            new FakeAuthService(new DemoUser("demo@test.com", "Shopper")),
            catalog);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateStockAsync("canvas-tote", 5, TestContext.Current.CancellationToken));
        Assert.Equal(0, catalog.UpdateCount);
    }

    [Fact]
    public async Task Unauthenticated_user_cannot_update_product_stock()
    {
        var service = new InventoryManagementService(new FakeAuthService(null), new FakeInventoryCatalog());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStockAsync("canvas-tote", 5, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Negative_stock_is_rejected()
    {
        var catalog = new FakeInventoryCatalog();
        var service = new InventoryManagementService(
            new FakeAuthService(new DemoUser("admin@test.com", "Manager", DemoRole.StoreManager)),
            catalog);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateStockAsync("canvas-tote", -1, TestContext.Current.CancellationToken));
        Assert.Equal(0, catalog.UpdateCount);
    }

    private sealed class FakeAuthService(DemoUser? user) : IDemoAuthService
    {
        public DemoUser? CurrentUser { get; } = user;
        public event Action? AuthenticationStateChanged
        {
            add { }
            remove { }
        }

        public Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public void Logout() { }
    }

    private sealed class FakeInventoryCatalog : IInventoryCatalog
    {
        private Product _product = new()
        {
            Id = "canvas-tote",
            Name = "Market Canvas Tote",
            Description = "A reusable tote.",
            Category = "Accessories",
            Price = 19.95m,
            ImageUrl = "images/product-placeholder.svg",
            Stock = 0
        };

        public int UpdateCount { get; private set; }

        public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>([_product]);

        public Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(_product.Id == productId ? _product : null);

        public Task<Product> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            _product = _product with { Stock = stock };
            return Task.FromResult(_product);
        }
    }
}
