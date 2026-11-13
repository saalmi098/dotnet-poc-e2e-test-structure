using ShopDemo.Domain;

namespace ShopDemo.Contracts.Orders;

public sealed record PlaceOrderRequest(
    string CustomerName,
    string Email,
    string AddressLine,
    string PostalCode,
    string City,
    string Country,
    ShippingMethod ShippingMethod,
    PaymentMethod PaymentMethod,
    string? CardNumber = null,
    string? CardHolder = null,
    string? CardExpiry = null,
    string? CardCvc = null,
    bool SaveAddress = false);
