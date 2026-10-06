namespace ShopDemo.Domain;

public sealed class Cart
{
    private readonly Dictionary<string, CartLine> _items = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<CartLine> Items => [.. _items.Values.OrderBy(item => item.Product.Name, StringComparer.OrdinalIgnoreCase)];

    public int ItemCount => _items.Values.Sum(item => item.Quantity);

    public decimal Subtotal => _items.Values.Sum(item => item.LineTotal);

    public void Add(Product product, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(product);
        ValidateQuantity(product, quantity);

        var newQuantity = quantity;
        if (_items.TryGetValue(product.Id, out var existing))
        {
            newQuantity = checked(existing.Quantity + quantity);
        }

        EnsureAvailableQuantity(product, newQuantity);
        _items[product.Id] = new CartLine(product, newQuantity);
    }

    public void SetQuantity(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        if (quantity == 0)
        {
            Remove(product.Id);
            return;
        }

        EnsureAvailableQuantity(product, quantity);
        _items[product.Id] = new CartLine(product, quantity);
    }

    public void Remove(string productId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        _items.Remove(productId);
    }

    public void Clear() => _items.Clear();

    private static void ValidateQuantity(Product product, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        EnsureAvailableQuantity(product, quantity);
    }

    private static void EnsureAvailableQuantity(Product product, int quantity)
    {
        if (!product.IsAvailable)
        {
            throw new InvalidOperationException($"{product.Name} is out of stock.");
        }

        if (quantity > product.Stock)
        {
            throw new InvalidOperationException($"Only {product.Stock} unit(s) of {product.Name} are available.");
        }
    }
}

public sealed record CartLine(Product Product, int Quantity)
{
    public decimal LineTotal => Product.Price * Quantity;
}
