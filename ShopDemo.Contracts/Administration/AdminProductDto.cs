namespace ShopDemo.Contracts;

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
