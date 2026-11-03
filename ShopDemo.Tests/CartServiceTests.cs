using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;
using Xunit;

namespace ShopDemo.Tests;

public sealed class CartServiceTests : IDisposable
{
    private readonly TestShop _shop = new();

    public void Dispose() => _shop.Dispose();

    private CartOwner Demo => CartOwner.ForUser(_shop.DemoShopperId);

    [Fact]
    public async Task Adding_a_product_creates_the_cart_and_totals_it()
    {
        var cart = await _shop.Carts.AddAsync(Demo, "ceramic-mug", 2);

        var line = Assert.Single(cart.Items);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(48m, cart.Subtotal);
        Assert.Equal(2, cart.ItemCount);
        Assert.Equal(Pricing.StandardShippingCost, cart.ShippingCost);
    }

    [Fact]
    public async Task Adding_the_same_product_again_increases_the_quantity()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");
        var cart = await _shop.Carts.AddAsync(Demo, "ceramic-mug", 2);

        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public async Task Out_of_stock_products_cannot_be_added()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Carts.AddAsync(Demo, "canvas-tote"));

        Assert.Contains("out of stock", exception.Message);
    }

    [Fact]
    public async Task Quantity_cannot_exceed_the_available_stock()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Carts.AddAsync(Demo, "desk-lamp", 7));

        Assert.Contains("Only 6", exception.Message);
    }

    [Fact]
    public async Task Quantity_cannot_exceed_the_per_line_maximum()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Carts.AddAsync(Demo, "pencil-set", 11));

        Assert.Contains("at most 10", exception.Message);
    }

    [Fact]
    public async Task Unknown_products_are_rejected()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Carts.AddAsync(Demo, "does-not-exist"));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task Setting_the_quantity_to_zero_removes_the_line()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        var cart = await _shop.Carts.SetQuantityAsync(Demo, "ceramic-mug", 0);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task Guest_cart_is_merged_into_the_account_cart_on_login()
    {
        var guestId = Guid.NewGuid();
        var guest = CartOwner.ForGuest(guestId);
        await _shop.Carts.AddAsync(guest, "ceramic-mug", 1);
        await _shop.Carts.AddAsync(guest, "field-notebook", 2);
        await _shop.Carts.AddAsync(Demo, "ceramic-mug", 2);

        await _shop.Carts.MergeGuestCartAsync(guestId, _shop.DemoShopperId);

        var merged = await _shop.Carts.GetAsync(Demo);
        Assert.Equal(3, merged.Items.Single(line => line.ProductId == "ceramic-mug").Quantity);
        Assert.Equal(2, merged.Items.Single(line => line.ProductId == "field-notebook").Quantity);
        Assert.Empty((await _shop.Carts.GetAsync(guest)).Items);
    }

    [Fact]
    public async Task Coupon_is_applied_and_changes_the_total()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp", 2);

        var cart = await _shop.Carts.ApplyCouponAsync(Demo, "welcome10");

        Assert.Equal("WELCOME10", cart.CouponCode);
        Assert.Equal(9.2m, cart.Discount);
    }

    [Theory]
    [InlineData("NOSUCHCODE", "not found")]
    [InlineData("OLD20", "expired")]
    [InlineData("ONCE", "fully redeemed")]
    [InlineData("SAVE5", "minimum")]
    public async Task Invalid_coupons_are_rejected_with_a_reason(string code, string expected)
    {
        await _shop.Carts.AddAsync(Demo, "pencil-set");

        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Carts.ApplyCouponAsync(Demo, code));

        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public async Task Coupon_is_dropped_when_the_cart_no_longer_qualifies()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp");
        await _shop.Carts.ApplyCouponAsync(Demo, "SAVE5");

        var cart = await _shop.Carts.SetQuantityAsync(Demo, "desk-lamp", 0);
        await _shop.Carts.AddAsync(Demo, "pencil-set");

        Assert.Null((await _shop.Carts.GetAsync(Demo)).CouponCode);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task Express_shipping_quote_includes_the_express_fee()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp", 2);

        var cart = await _shop.Carts.GetAsync(Demo, ShippingMethod.Express);

        Assert.Equal(Pricing.ExpressShippingCost, cart.ShippingCost);
        Assert.Equal(92m + Pricing.ExpressShippingCost, cart.Total);
    }
}
