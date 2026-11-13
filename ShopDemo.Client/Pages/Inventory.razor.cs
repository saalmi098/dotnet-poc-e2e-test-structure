namespace ShopDemo.Client.Pages;

public partial class Inventory
{
    private IReadOnlyList<AdminProductDto> _products = [];
    private readonly Dictionary<string, int> _pendingStocks = new(StringComparer.OrdinalIgnoreCase);
    private string? _error;
    private bool _loading = true;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectUnlessManager("/manage/inventory"))
        {
            return;
        }

        try
        {
            _products = await Api.GetAdminProductsAsync();
            foreach (var product in _products)
            {
                _pendingStocks[product.Id] = product.Stock;
            }
        }
        catch (ApiException exception)
        {
            _error = $"Inventory could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void SetPendingStock(string productId, int stock) => _pendingStocks[productId] = stock;

    private async Task SaveStockAsync(AdminProductDto product)
    {
        _saving = true;
        _error = null;
        try
        {
            var updatedProduct = await Api.UpdateStockAsync(product.Id, _pendingStocks[product.Id]);
            _products = [.. _products.Select(current => current.Id == updatedProduct.Id ? updatedProduct : current)];
            Snackbar.Add($"{updatedProduct.Name} stock updated to {updatedProduct.Stock}.", Severity.Success);
        }
        catch (ApiException exception)
        {
            _error = $"Stock could not be updated. {exception.Message}";
        }
        finally
        {
            _saving = false;
        }
    }
}
