using ShopDemo.Domain;

namespace ShopDemo.Contracts;

public sealed record CartDto(
    IReadOnlyList<CartLineDto> Items,
    int ItemCount,
    string? CouponCode,
    decimal Subtotal,
    decimal Discount,
    decimal ShippingCost,
    decimal Total,
    decimal IncludedVat,
    ShippingMethod ShippingMethod,
    decimal FreeShippingRemaining);
