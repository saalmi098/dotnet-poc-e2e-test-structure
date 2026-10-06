using ShopDemo.Domain;

namespace ShopDemo.Application;

public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default);
}

public interface IInventoryCatalog : IProductCatalog
{
    Task<Product> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default);
}

public interface IInventoryManagementService
{
    Task<Product> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default);
}

public interface ICartService
{
    IReadOnlyList<CartLine> Items { get; }
    int ItemCount { get; }
    decimal Subtotal { get; }
    event Action? CartChanged;
    Task AddProductAsync(string productId, int quantity = 1, CancellationToken cancellationToken = default);
    Task SetQuantityAsync(string productId, int quantity, CancellationToken cancellationToken = default);
    void RemoveProduct(string productId);
    void Clear();
}

public interface IDemoAuthService
{
    DemoUser? CurrentUser { get; }
    event Action? AuthenticationStateChanged;
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    void Logout();
}

public interface IOrderStore
{
    IReadOnlyList<Order> GetByAccount(string accountEmail);
    IReadOnlyList<Order> GetAll();
    void Add(Order order);
}

public interface ICheckoutService
{
    IReadOnlyList<Order> GetOrdersForCurrentUser();
    Order PlaceOrder(CheckoutDetails details);
}
