using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShopDemo.Application.Interfaces;

namespace ShopDemo.Client.Pages;

public partial class Cart
{
    [Inject]
    private IDemoAuthService Auth { get; set; } = default!;

    private string CheckoutHref => Auth.CurrentUser is null
        ? "/login?returnUrl=%2Fcheckout"
        : "/checkout";

    private async Task UpdateQuantityAsync(string productId, int quantity)
    {
        try
        {
            await CartService.SetQuantityAsync(productId, quantity);
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private void RemoveProduct(string productId) => CartService.RemoveProduct(productId);
}