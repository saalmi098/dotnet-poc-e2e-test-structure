using MudBlazor;
using ShopDemo.Domain;

namespace ShopDemo.Client.Pages;

public partial class Home
{
    private IReadOnlyList<Product> _products = [];
    private IReadOnlyList<string> _categories = [];
    private string _searchText = string.Empty;
    private string _selectedCategory = string.Empty;
    private string? _error;
    private bool _loading = true;

    private IEnumerable<Product> FilteredProducts => _products.Where(product =>
        (string.IsNullOrWhiteSpace(_searchText) ||
         product.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(_selectedCategory) ||
         string.Equals(product.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase)));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _products = await Catalog.GetProductsAsync();
            _categories = _products.Select(product => product.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            _error = $"The catalog could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task AddToCartAsync(string productId)
    {
        try
        {
            await Cart.AddProductAsync(productId);
            Snackbar.Add("Added to your cart.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}