namespace ShopDemo.Domain;

public sealed class Order
{
    public Order(
        string number,
        DateTimeOffset createdAt,
        string accountEmail,
        string customerName,
        string customerEmail,
        IEnumerable<OrderLine> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        ArgumentNullException.ThrowIfNull(items);

        Number = number;
        CreatedAt = createdAt;
        AccountEmail = accountEmail;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        Items = Array.AsReadOnly(items.ToArray());

        if (Items.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one item.", nameof(items));
        }
    }

    public string Number { get; }
    public DateTimeOffset CreatedAt { get; }
    public string AccountEmail { get; }
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public IReadOnlyList<OrderLine> Items { get; }
    public decimal Total => Items.Sum(item => item.LineTotal);
}

public sealed record OrderLine(string ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
