namespace ShopDemo.Contracts.Orders;

public sealed record OrderLineDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
