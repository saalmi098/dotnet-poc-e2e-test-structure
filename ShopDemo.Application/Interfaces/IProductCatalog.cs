using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default);
}
