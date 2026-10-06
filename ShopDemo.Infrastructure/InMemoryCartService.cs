using ShopDemo.Application;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure;

public sealed class InMemoryCartService(IProductCatalog productCatalog) : ICartService
{
    private readonly Cart _cart = new();

    public IReadOnlyList<CartLine> Items => _cart.Items;
    public int ItemCount => _cart.ItemCount;
    public decimal Subtotal => _cart.Subtotal;

    public event Action? CartChanged;

    public async Task AddProductAsync(
        string productId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        _cart.Add(product, quantity);
        CartChanged?.Invoke();
    }

    public async Task SetQuantityAsync(
        string productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        _cart.SetQuantity(product, quantity);
        CartChanged?.Invoke();
    }

    public void RemoveProduct(string productId)
    {
        _cart.Remove(productId);
        CartChanged?.Invoke();
    }

    public void Clear()
    {
        _cart.Clear();
        CartChanged?.Invoke();
    }

    private async Task<Product> GetProductOrThrowAsync(string productId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        return await productCatalog.GetProductAsync(productId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");
    }
}
