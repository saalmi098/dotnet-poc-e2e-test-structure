namespace ShopDemo.Domain;

public sealed record PriceBreakdown(
    decimal Subtotal,
    decimal Discount,
    decimal ShippingCost,
    decimal Total,
    decimal IncludedVat);

public static class Pricing
{
    public const decimal VatRate = 0.20m;
    public const decimal StandardShippingCost = 4.90m;
    public const decimal ExpressShippingCost = 12.90m;
    public const decimal FreeStandardShippingThreshold = 50m;
    public const int MaxQuantityPerLine = 10;

    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static decimal ShippingCostFor(ShippingMethod method, decimal discountedSubtotal) => method switch
    {
        ShippingMethod.Standard => discountedSubtotal >= FreeStandardShippingThreshold ? 0m : StandardShippingCost,
        ShippingMethod.Express => ExpressShippingCost,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    public static PriceBreakdown Calculate(decimal subtotal, Coupon? coupon, ShippingMethod shippingMethod)
    {
        subtotal = Round(subtotal);
        var discount = coupon is null ? 0m : DiscountFor(coupon, subtotal);
        var shipping = coupon?.Type == CouponType.FreeShipping
            ? 0m
            : ShippingCostFor(shippingMethod, subtotal - discount);
        var total = Round(subtotal - discount + shipping);
        var vat = Round(total - total / (1 + VatRate));
        return new PriceBreakdown(subtotal, discount, shipping, total, vat);
    }

    public static decimal DiscountFor(Coupon coupon, decimal subtotal)
    {
        var discount = coupon.Type switch
        {
            CouponType.Percentage => subtotal * coupon.Value / 100m,
            CouponType.FixedAmount => coupon.Value,
            _ => 0m
        };
        return Round(Math.Min(discount, subtotal));
    }

    /// <summary>Throws a <see cref="ShopException"/> explaining why <paramref name="coupon"/> cannot be applied.</summary>
    public static void EnsureCouponUsable(Coupon coupon, decimal subtotal, DateTimeOffset now)
    {
        if (!coupon.IsActive)
        {
            throw new ShopException($"Coupon {coupon.Code} is not active.");
        }

        if (coupon.ExpiresAt is { } expiresAt && expiresAt <= now)
        {
            throw new ShopException($"Coupon {coupon.Code} has expired.");
        }

        if (coupon.MaxRedemptions is { } max && coupon.Redemptions >= max)
        {
            throw new ShopException($"Coupon {coupon.Code} has been fully redeemed.");
        }

        if (subtotal < coupon.MinSubtotal)
        {
            throw new ShopException($"Coupon {coupon.Code} requires a minimum order of {coupon.MinSubtotal:0.00}.");
        }
    }
}

public static class OrderRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus current) => Transitions[current];

    public static bool CanTransition(OrderStatus from, OrderStatus to) => Transitions[from].Contains(to);

    /// <summary>Shoppers may cancel only before the order has shipped.</summary>
    public static bool CanShopperCancel(OrderStatus status) => status is OrderStatus.Pending or OrderStatus.Processing;

    public static string NewOrderNumber() => $"ORD-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    public static bool IsValidCardNumber(string? cardNumber)
    {
        var digits = (cardNumber ?? string.Empty).Replace(" ", "").Replace("-", "");
        if (digits.Length is < 13 or > 19 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}
