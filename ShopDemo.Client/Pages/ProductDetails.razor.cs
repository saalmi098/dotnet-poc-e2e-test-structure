using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShopDemo.Domain;

namespace ShopDemo.Client.Pages;

public partial class ProductDetails
{
    [Parameter]
    public string ProductId { get; set; } = string.Empty;

    private Product? _product;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _product = null;
        try
        {
            _product = await Catalog.GetProductAsync(ProductId);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Product details could not be loaded. {exception.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task AddToCartAsync()
    {
        try
        {
            await Cart.AddProductAsync(ProductId);
            Snackbar.Add("Added to your cart.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}