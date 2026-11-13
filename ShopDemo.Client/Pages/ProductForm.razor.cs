using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class ProductForm
{
    [Parameter]
    public string? ProductId { get; set; }

    private readonly ProductModel _model = new();
    private IReadOnlyList<string> _categories = [];
    private string? _error;
    private string? _loadError;
    private bool _loading = true;

    private bool IsNew => string.IsNullOrEmpty(ProductId);

    protected override async Task OnParametersSetAsync()
    {
        if (RedirectUnlessManager(IsNew ? "/manage/products/new" : $"/manage/products/{ProductId}"))
        {
            return;
        }

        _loading = true;
        try
        {
            _categories = [.. (await Api.GetAdminProductsAsync()).Select(product => product.Category).Distinct().Order()];
            if (!IsNew)
            {
                var product = await Api.GetAdminProductAsync(ProductId!);
                _model.Id = product.Id;
                _model.Name = product.Name;
                _model.Description = product.Description;
                _model.Category = product.Category;
                _model.Price = product.Price;
                _model.OriginalPrice = product.OriginalPrice;
                _model.Stock = product.Stock;
                _model.ImageUrl = product.ImageUrl;
                _model.IsActive = product.IsActive;
            }
        }
        catch (ApiException exception)
        {
            _loadError = exception.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveAsync()
    {
        _error = null;
        var category = string.IsNullOrWhiteSpace(_model.NewCategory) ? _model.Category : _model.NewCategory.Trim();
        if (string.IsNullOrWhiteSpace(category))
        {
            _error = "Choose a category or enter a new one.";
            return;
        }

        var request = new SaveProductRequest(
            _model.Id, _model.Name, _model.Description, category, _model.Price,
            _model.OriginalPrice, _model.ImageUrl, _model.Stock, _model.IsActive);
        try
        {
            if (IsNew)
            {
                await Api.CreateProductAsync(request);
            }
            else
            {
                await Api.UpdateProductAsync(ProductId!, request);
            }

            Snackbar.Add($"{_model.Name} was saved.", Severity.Success);
            Navigation.NavigateTo("/manage/products");
        }
        catch (ApiException exception)
        {
            _error = exception.Message;
        }
    }

    private sealed class ProductModel
    {
        [Required(ErrorMessage = "Enter a product id.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a name.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a description.")]
        public string Description { get; set; } = string.Empty;

        public string? Category { get; set; }
        public string? NewCategory { get; set; }

        [Range(0.0, 100000.0, ErrorMessage = "Enter a price of at least 0.")]
        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        [Range(0, 100000, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
