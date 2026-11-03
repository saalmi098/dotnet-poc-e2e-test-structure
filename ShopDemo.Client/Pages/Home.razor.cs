namespace ShopDemo.Client.Pages;

public partial class Home
{
    private const int PageSize = 12;

    private PagedResult<ProductSummaryDto>? _result;
    private IReadOnlyList<CategoryDto> _categories = [];
    private string? _searchText = string.Empty;
    private string _selectedCategory = string.Empty;
    private ProductSort _sort = ProductSort.Featured;
    private decimal? _minPrice;
    private decimal? _maxPrice;
    private bool _inStockOnly;
    private bool _onSaleOnly;
    private int _page = 1;
    private string? _error;
    private bool _loading = true;
    private int _requestCounter;

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(_searchText) || !string.IsNullOrEmpty(_selectedCategory) || _sort != ProductSort.Featured ||
        _minPrice is not null || _maxPrice is not null || _inStockOnly || _onSaleOnly;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await Api.GetCategoriesAsync();
        }
        catch (ApiException exception)
        {
            _error = $"The catalog could not be loaded. {exception.Message}";
            _loading = false;
            return;
        }

        await LoadAsync();
    }

    private async Task OnFilterChangedAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    private async Task OnPageChangedAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _searchText = string.Empty;
        _selectedCategory = string.Empty;
        _sort = ProductSort.Featured;
        _minPrice = null;
        _maxPrice = null;
        _inStockOnly = false;
        _onSaleOnly = false;
        await OnFilterChangedAsync();
    }

    private async Task LoadAsync()
    {
        // Ignore responses that arrive after a newer request was started (fast typing).
        var request = ++_requestCounter;
        try
        {
            var result = await Api.GetProductsAsync(new ProductQuery(
                _searchText, _selectedCategory, _minPrice, _maxPrice, _inStockOnly, _onSaleOnly, _sort, _page, PageSize));
            if (request != _requestCounter)
            {
                return;
            }

            _result = result;
            _error = null;
        }
        catch (ApiException exception)
        {
            if (request == _requestCounter)
            {
                _error = $"The catalog could not be loaded. {exception.Message}";
            }
        }
        finally
        {
            if (request == _requestCounter)
            {
                _loading = false;
            }
        }
    }

    private async Task ToggleWishlistAsync(ProductSummaryDto product)
    {
        if (RedirectToLoginIfAnonymous("/"))
        {
            return;
        }

        var added = !product.IsInWishlist;
        var ok = await TryAsync(
            () => added ? Api.AddToWishlistAsync(product.Id) : Api.RemoveFromWishlistAsync(product.Id),
            added ? $"{product.Name} added to your wishlist." : $"{product.Name} removed from your wishlist.");
        if (ok && _result is not null)
        {
            _result = _result with
            {
                Items = [.. _result.Items.Select(item => item.Id == product.Id ? item with { IsInWishlist = added } : item)]
            };
        }
    }
}
