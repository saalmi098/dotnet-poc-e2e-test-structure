using ShopDemo.Domain;
using Xunit;

namespace ShopDemo.Tests;

public sealed class PricingTests
{
    private static Coupon Percent(decimal value, decimal min = 0) =>
        new() { Code = "P", Type = CouponType.Percentage, Value = value, MinSubtotal = min };

    [Fact]
    public void Standard_shipping_is_charged_below_the_free_shipping_threshold()
    {
        var price = Pricing.Calculate(30m, null, ShippingMethod.Standard);

        Assert.Equal(4.90m, price.ShippingCost);
        Assert.Equal(34.90m, price.Total);
    }

    [Fact]
    public void Standard_shipping_is_free_from_the_threshold()
    {
        var price = Pricing.Calculate(50m, null, ShippingMethod.Standard);

        Assert.Equal(0m, price.ShippingCost);
        Assert.Equal(50m, price.Total);
    }

    [Fact]
    public void Express_shipping_is_always_charged()
    {
        var price = Pricing.Calculate(200m, null, ShippingMethod.Express);

        Assert.Equal(Pricing.ExpressShippingCost, price.ShippingCost);
    }

    [Fact]
    public void Percentage_coupon_reduces_the_subtotal()
    {
        var price = Pricing.Calculate(100m, Percent(10), ShippingMethod.Standard);

        Assert.Equal(10m, price.Discount);
        Assert.Equal(90m, price.Total);
    }

    [Fact]
    public void Discount_can_push_the_order_below_the_free_shipping_threshold()
    {
        var price = Pricing.Calculate(52m, Percent(10), ShippingMethod.Standard);

        Assert.Equal(Pricing.StandardShippingCost, price.ShippingCost);
    }

    [Fact]
    public void Fixed_coupon_never_exceeds_the_subtotal()
    {
        var coupon = new Coupon { Code = "F", Type = CouponType.FixedAmount, Value = 50m };

        Assert.Equal(20m, Pricing.DiscountFor(coupon, 20m));
    }

    [Fact]
    public void Free_shipping_coupon_removes_shipping_even_for_express()
    {
        var coupon = new Coupon { Code = "S", Type = CouponType.FreeShipping };

        var price = Pricing.Calculate(10m, coupon, ShippingMethod.Express);

        Assert.Equal(0m, price.ShippingCost);
        Assert.Equal(10m, price.Total);
    }

    [Fact]
    public void Included_vat_is_a_fifth_of_the_net_plus_vat_total()
    {
        var price = Pricing.Calculate(120m, null, ShippingMethod.Standard);

        Assert.Equal(20m, price.IncludedVat);
    }

    [Theory]
    [InlineData(false, null, null, 0, "not active")]
    [InlineData(true, -1, null, 0, "expired")]
    [InlineData(true, null, 1, 1, "fully redeemed")]
    public void Unusable_coupons_explain_why(bool active, int? expiresInDays, int? maxRedemptions, int redemptions, string expected)
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = new Coupon
        {
            Code = "X",
            IsActive = active,
            ExpiresAt = expiresInDays is { } days ? now.AddDays(days) : null,
            MaxRedemptions = maxRedemptions,
            Redemptions = redemptions
        };

        var exception = Assert.Throws<ShopException>(() => Pricing.EnsureCouponUsable(coupon, 100m, now));

        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public void Coupon_requires_its_minimum_subtotal()
    {
        var exception = Assert.Throws<ShopException>(() =>
            Pricing.EnsureCouponUsable(Percent(5, min: 30), 20m, DateTimeOffset.UtcNow));

        Assert.Contains("minimum", exception.Message);
    }
}

public sealed class OrderRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    public void Status_transitions_follow_the_fulfilment_flow(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderRules.CanTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void Shoppers_can_cancel_only_before_shipping(OrderStatus status, bool allowed) =>
        Assert.Equal(allowed, OrderRules.CanShopperCancel(status));

    [Theory]
    [InlineData("4242 4242 4242 4242", true)]
    [InlineData("4000000000000002", true)]
    [InlineData("4242 4242 4242 4241", false)]
    [InlineData("1234", false)]
    [InlineData("abcd efgh ijkl mnop", false)]
    [InlineData(null, false)]
    public void Card_numbers_are_checked_with_the_luhn_algorithm(string? number, bool valid) =>
        Assert.Equal(valid, OrderRules.IsValidCardNumber(number));

    [Fact]
    public void Order_numbers_have_a_stable_readable_format()
    {
        var number = OrderRules.NewOrderNumber();

        Assert.Matches("^ORD-[0-9A-F]{8}$", number);
    }
}
