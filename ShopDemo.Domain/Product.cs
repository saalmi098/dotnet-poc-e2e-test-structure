namespace ShopDemo.Domain;

public sealed record Product
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int Stock { get; init; }

    public bool IsAvailable => Stock > 0;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(ImageUrl);

        if (Price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Price), "Product price cannot be negative.");
        }

        if (Stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Stock), "Product stock cannot be negative.");
        }
    }
}
