using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

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
