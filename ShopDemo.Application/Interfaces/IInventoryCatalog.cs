using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface IInventoryCatalog : IProductCatalog
{
    Task<Product> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default);
}
