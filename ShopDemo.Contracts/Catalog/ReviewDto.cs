namespace ShopDemo.Contracts.Catalog;

public sealed record ReviewDto(
    int Id,
    string ProductId,
    int UserId,
    string AuthorName,
    int Rating,
    string Title,
    string Comment,
    DateTimeOffset CreatedAt);
