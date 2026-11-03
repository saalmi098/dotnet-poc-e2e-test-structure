using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Checkout
{
    private readonly CheckoutModel _model = new();
    private CartDto _cart = new([], 0, null, 0, 0, 0, 0, 0, ShippingMethod.Standard, 0);
    private string? _error;
    private OrderDto? _order;
    private bool _loading = true;
    private bool _submitting;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectToLoginIfAnonymous("/checkout"))
        {
            return;
        }

        if (Auth.IsManager)
        {
            Navigation.NavigateTo("/");
            return;
        }

        var user = Auth.CurrentUser!;
        _model.CustomerName = user.DisplayName;
        _model.Email = user.Email;
        _model.AddressLine = user.AddressLine ?? string.Empty;
        _model.PostalCode = user.PostalCode ?? string.Empty;
        _model.City = user.City ?? string.Empty;
        _model.Country = user.Country ?? string.Empty;

        await TryAsync(async () => _cart = await Basket.RefreshAsync(_model.ShippingMethod));
        _loading = false;
    }

    private async Task OnShippingChangedAsync(ShippingMethod method)
    {
        _model.ShippingMethod = method;
        await TryAsync(async () => _cart = await Basket.RefreshAsync(method));
    }

    private async Task SubmitOrderAsync()
    {
        _error = null;
        _submitting = true;
        try
        {
            _order = await Api.PlaceOrderAsync(new PlaceOrderRequest(
                _model.CustomerName, _model.Email, _model.AddressLine, _model.PostalCode, _model.City, _model.Country,
                _model.ShippingMethod, _model.PaymentMethod,
                _model.CardNumber, _model.CardHolder, _model.CardExpiry, _model.CardCvc, _model.SaveAddress));
            await Basket.RefreshAsync();
            if (_model.SaveAddress && Auth.CurrentUser is { } user)
            {
                Auth.UpdateUser(user with
                {
                    AddressLine = _model.AddressLine,
                    PostalCode = _model.PostalCode,
                    City = _model.City,
                    Country = _model.Country
                });
            }
        }
        catch (ApiException exception)
        {
            _error = exception.Message;
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class CheckoutModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a street address.")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a postal code.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a city.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a country.")]
        public string Country { get; set; } = string.Empty;

        public bool SaveAddress { get; set; }
        public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Invoice;
        public string? CardHolder { get; set; }
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvc { get; set; }
    }
}
