namespace ShopDemo.Client.Pages;

public partial class Cart
{
    private CartDto _cart = new([], 0, null, 0, 0, 0, 0, 0, ShippingMethod.Standard, 0);
    private bool _loading = true;
    private string? _couponCode;
    private string? _couponError;

    private string CheckoutHref => Auth.CurrentUser is null
        ? "/login?returnUrl=%2Fcheckout"
        : "/checkout";

    protected override async Task OnInitializedAsync()
    {
        Basket.Changed += OnCartChanged;
        await TryAsync(async () => _cart = await Basket.RefreshAsync());
        _loading = false;
    }

    private void OnCartChanged()
    {
        _cart = Basket.Cart;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task UpdateQuantityAsync(string productId, int quantity) =>
        await TryAsync(() => Basket.SetQuantityAsync(productId, quantity));

    private async Task RemoveProduct(string productId) => await TryAsync(() => Basket.RemoveAsync(productId));

    private async Task ApplyCouponAsync()
    {
        _couponError = null;
        try
        {
            await Basket.ApplyCouponAsync(_couponCode ?? string.Empty);
            _couponCode = null;
            Snackbar.Add("Coupon applied.", Severity.Success);
        }
        catch (ApiException exception)
        {
            _couponError = exception.Message;
        }
    }

    private async Task RemoveCouponAsync() => await TryAsync(Basket.RemoveCouponAsync);

    public void Dispose() => Basket.Changed -= OnCartChanged;
}
