using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface IInventoryManagementService
{
    Task<Product> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default);
}
