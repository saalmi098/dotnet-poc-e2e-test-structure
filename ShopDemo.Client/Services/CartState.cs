using ShopDemo.Contracts;
using ShopDemo.Domain;

namespace ShopDemo.Client.Services;

/// <summary>Client-side view of the server-held cart; raises <see cref="Changed"/> so the header badge updates.</summary>
public sealed class CartState
{
    private readonly ShopApi _api;

    public CartState(ShopApi api, AuthState auth)
    {
        _api = api;
        // The server merges the guest cart on login and the account cart disappears on logout.
        auth.Changed += () => _ = RefreshSafelyAsync();
    }

    public CartDto Cart { get; private set; } = Empty;

    public int ItemCount => Cart.ItemCount;

    public event Action? Changed;

    private static CartDto Empty { get; } =
        new([], 0, null, 0, 0, 0, 0, 0, ShippingMethod.Standard, Pricing.FreeStandardShippingThreshold);

    public async Task<CartDto> RefreshAsync(ShippingMethod shipping = ShippingMethod.Standard)
    {
        Set(await _api.GetCartAsync(shipping));
        return Cart;
    }

    public async Task AddAsync(string productId, int quantity = 1) => Set(await _api.AddToCartAsync(productId, quantity));

    public async Task SetQuantityAsync(string productId, int quantity) =>
        Set(await _api.SetCartQuantityAsync(productId, quantity));

    public async Task RemoveAsync(string productId) => Set(await _api.RemoveFromCartAsync(productId));

    public async Task ApplyCouponAsync(string code) => Set(await _api.ApplyCouponAsync(code));

    public async Task RemoveCouponAsync() => Set(await _api.RemoveCouponAsync());

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (ApiException)
        {
            Set(Empty);
        }
    }

    private void Set(CartDto cart)
    {
        Cart = cart;
        Changed?.Invoke();
    }
}
