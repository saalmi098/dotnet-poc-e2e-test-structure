using ShopDemo.Domain;

namespace ShopDemo.Contracts;

// ---- Accounts -------------------------------------------------------------------------------

public sealed record UserDto(
    int Id,
    string Email,
    string DisplayName,
    UserRole Role,
    string? Phone,
    string? AddressLine,
    string? PostalCode,
    string? City,
    string? Country);

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string DisplayName, string Email, string Password);

public sealed record UpdateProfileRequest(
    string DisplayName,
    string? Phone,
    string? AddressLine,
    string? PostalCode,
    string? City,
    string? Country);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

// ---- Catalog --------------------------------------------------------------------------------

public sealed record CategoryDto(int Id, string Name, int ProductCount);

public sealed record ProductSummaryDto(
    string Id,
    string Name,
    string Description,
    string Category,
    decimal Price,
    decimal? OriginalPrice,
    string ImageUrl,
    int Stock,
    double AverageRating,
    int ReviewCount,
    bool IsInWishlist)
{
    public bool IsAvailable => Stock > 0;
    public bool IsOnSale => OriginalPrice is { } original && original > Price;
}

public sealed record ProductDetailDto(
    string Id,
    string Name,
    string Description,
    string Category,
    decimal Price,
    decimal? OriginalPrice,
    string ImageUrl,
    int Stock,
    double AverageRating,
    int ReviewCount,
    bool IsInWishlist,
    IReadOnlyList<int> RatingDistribution)
{
    public bool IsAvailable => Stock > 0;
    public bool IsOnSale => OriginalPrice is { } original && original > Price;
}

public enum ProductSort
{
    Featured,
    PriceAscending,
    PriceDescending,
    NameAscending,
    TopRated,
    Newest
}

public sealed record ProductQuery(
    string? Search = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    bool OnSaleOnly = false,
    ProductSort Sort = ProductSort.Featured,
    int Page = 1,
    int PageSize = 12);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ReviewDto(
    int Id,
    string ProductId,
    int UserId,
    string AuthorName,
    int Rating,
    string Title,
    string Comment,
    DateTimeOffset CreatedAt);

public sealed record CreateReviewRequest(int Rating, string Title, string Comment);

// ---- Cart -----------------------------------------------------------------------------------

public sealed record CartLineDto(
    string ProductId,
    string ProductName,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    int Stock)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

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

public sealed record AddCartItemRequest(string ProductId, int Quantity = 1);

public sealed record SetCartQuantityRequest(int Quantity);

public sealed record ApplyCouponRequest(string Code);

// ---- Orders ---------------------------------------------------------------------------------

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

public sealed record OrderLineDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record OrderStatusEntryDto(OrderStatus Status, DateTimeOffset At, string? Note);

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

public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? Note = null);

// ---- Administration -------------------------------------------------------------------------

public sealed record AdminProductDto(
    string Id,
    string Name,
    string Description,
    string Category,
    decimal Price,
    decimal? OriginalPrice,
    string ImageUrl,
    int Stock,
    bool IsActive)
{
    public bool IsAvailable => Stock > 0;
}

public sealed record SaveProductRequest(
    string Id,
    string Name,
    string Description,
    string Category,
    decimal Price,
    decimal? OriginalPrice,
    string? ImageUrl,
    int Stock,
    bool IsActive = true);

public sealed record UpdateStockRequest(int Stock);

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

public sealed record SaveCouponRequest(
    string Code,
    string Description,
    CouponType Type,
    decimal Value,
    decimal MinSubtotal,
    DateTimeOffset? ExpiresAt,
    int? MaxRedemptions,
    bool IsActive = true);

public sealed record DashboardDto(
    int TotalOrders,
    int OpenOrders,
    decimal Revenue,
    int ProductCount,
    int LowStockCount,
    int OutOfStockCount,
    int CustomerCount,
    IReadOnlyList<AdminProductDto> LowStockProducts);
