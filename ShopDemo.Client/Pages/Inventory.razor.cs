using MudBlazor;
using ShopDemo.Domain;

namespace ShopDemo.Client.Pages;

public partial class Inventory
{
    private IReadOnlyList<Product> _products = [];
    private readonly Dictionary<string, int> _pendingStocks = new(StringComparer.OrdinalIgnoreCase);
    private string? _error;
    private bool _loading = true;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        if (Auth.CurrentUser is null)
        {
            Navigation.NavigateTo("/login?returnUrl=%2Fmanage%2Finventory");
            return;
        }

        if (Auth.CurrentUser.Role != DemoRole.StoreManager)
        {
            Navigation.NavigateTo("/");
            return;
        }

        try
        {
            _products = await Catalog.GetProductsAsync();
            foreach (var product in _products)
            {
                _pendingStocks[product.Id] = product.Stock;
            }
        }
        catch (Exception exception)
        {
            _error = $"Inventory could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void SetPendingStock(string productId, int stock) => _pendingStocks[productId] = stock;

    private async Task SaveStockAsync(Product product)
    {
        _saving = true;
        _error = null;
        try
        {
            var updatedProduct = await InventoryService.UpdateStockAsync(product.Id, _pendingStocks[product.Id]);
            _products = [.. _products.Select(current => current.Id == updatedProduct.Id ? updatedProduct : current)];
            Snackbar.Add($"{updatedProduct.Name} stock updated to {updatedProduct.Stock}.", Severity.Success);
        }
        catch (Exception exception)
        {
            _error = $"Stock could not be updated. {exception.Message}";
        }
        finally
        {
            _saving = false;
        }
    }
}
