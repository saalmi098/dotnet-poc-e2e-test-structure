using ShopDemo.Domain;

namespace ShopDemo.Contracts;

public sealed record SaveCouponRequest(
    string Code,
    string Description,
    CouponType Type,
    decimal Value,
    decimal MinSubtotal,
    DateTimeOffset? ExpiresAt,
    int? MaxRedemptions,
    bool IsActive = true);
