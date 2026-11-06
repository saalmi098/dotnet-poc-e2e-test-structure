using ShopDemo.Domain;

namespace ShopDemo.Contracts.Orders;

public sealed record OrderDto(
    string Number,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    string AccountEmail,
    string CustomerName,
    string CustomerEmail,
    string AddressLine,
    string PostalCode,
    string City,
    string Country,
    ShippingMethod ShippingMethod,
    PaymentMethod PaymentMethod,
    string PaymentReference,
    string? CouponCode,
    decimal Subtotal,
    decimal Discount,
    decimal ShippingCost,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<OrderStatusEntryDto> History,
    IReadOnlyList<OrderStatus> AllowedNextStatuses,
    bool CanCancel);
