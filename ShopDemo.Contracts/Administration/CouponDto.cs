using ShopDemo.Domain;

namespace ShopDemo.Contracts;

public sealed record CouponDto(
    string Code,
    string Description,
    CouponType Type,
    decimal Value,
    decimal MinSubtotal,
    DateTimeOffset? ExpiresAt,
    int? MaxRedemptions,
    int Redemptions,
    bool IsActive);
