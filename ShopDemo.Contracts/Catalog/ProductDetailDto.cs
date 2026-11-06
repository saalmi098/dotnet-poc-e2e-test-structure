namespace ShopDemo.Contracts.Catalog;

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
