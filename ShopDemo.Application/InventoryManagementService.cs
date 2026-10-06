using ShopDemo.Domain;

namespace ShopDemo.Application;

public sealed class InventoryManagementService(
    IDemoAuthService authService,
    IInventoryCatalog inventoryCatalog) : IInventoryManagementService
{
    public Task<Product> UpdateStockAsync(
        string productId,
        int stock,
        CancellationToken cancellationToken = default)
    {
        var user = authService.CurrentUser
            ?? throw new InvalidOperationException("Log in to manage inventory.");

        if (user.Role != DemoRole.StoreManager)
        {
            throw new UnauthorizedAccessException("Only a store manager can update inventory.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock cannot be negative.");
        }

        return inventoryCatalog.UpdateStockAsync(productId, stock, cancellationToken);
    }
}
