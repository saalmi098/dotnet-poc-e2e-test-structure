namespace ShopDemo.Contracts;

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
