namespace ShopDemo.Contracts;

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
