using ShopDemo.Domain;
using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Checkout
{
    private readonly CheckoutModel _model = new();
    private string? _error;
    private Order? _order;

    protected override void OnInitialized()
    {
        if (Auth.CurrentUser is null)
        {
            Navigation.NavigateTo("/login?returnUrl=%2Fcheckout");
            return;
        }

        if (Auth.CurrentUser.Role != DemoRole.Shopper)
        {
            Navigation.NavigateTo("/");
            return;
        }

        _model.CustomerName = Auth.CurrentUser.DisplayName;
        _model.Email = Auth.CurrentUser.Email;
    }

    private void SubmitOrder()
    {
        _error = null;
        try
        {
            _order = CheckoutService.PlaceOrder(new CheckoutDetails(_model.CustomerName, _model.Email));
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private sealed class CheckoutModel
    {
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}