namespace ShopDemo.Contracts;

public sealed record AddCartItemRequest(string ProductId, int Quantity = 1);
