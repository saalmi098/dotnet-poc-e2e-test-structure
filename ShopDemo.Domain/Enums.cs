using System.Text.Json.Serialization;

namespace ShopDemo.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Shopper,
    StoreManager
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShippingMethod
{
    Standard,
    Express
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Invoice,
    CreditCard
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouponType
{
    Percentage,
    FixedAmount,
    FreeShipping
}
