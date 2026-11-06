namespace ShopDemo.Contracts.Catalog;

public sealed record CreateReviewRequest(int Rating, string Title, string Comment);
