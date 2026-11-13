namespace ShopDemo.Client.Pages;

public partial class ManageProducts
{
    private IReadOnlyList<AdminProductDto> _products = [];
    private AdminProductDto? _deleting;
    private string? _filter;
    private string? _error;
    private bool _loading = true;

    private IEnumerable<AdminProductDto> FilteredProducts => string.IsNullOrWhiteSpace(_filter)
        ? _products
        : _products.Where(product =>
            product.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
            product.Id.Contains(_filter, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        if (RedirectUnlessManager("/manage/products"))
        {
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _products = await Api.GetAdminProductsAsync();
            _error = null;
        }
        catch (ApiException exception)
        {
            _error = $"Products could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ConfirmDelete(AdminProductDto product) => _deleting = product;

    private async Task DeleteAsync()
    {
        if (_deleting is null)
        {
            return;
        }

        var product = _deleting;
        _deleting = null;
        try
        {
            var deleted = await Api.DeleteProductAsync(product.Id);
            Snackbar.Add(deleted ? $"{product.Name} was deleted." : $"{product.Name} is part of existing orders and was deactivated instead.", Severity.Success);
            await LoadAsync();
        }
        catch (ApiException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}
