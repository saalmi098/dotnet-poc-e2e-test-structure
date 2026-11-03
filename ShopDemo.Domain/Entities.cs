namespace ShopDemo.Domain;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Shopper;
    public DateTimeOffset CreatedAt { get; set; }

    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal Price { get; set; }

    /// <summary>The price before a discount; when greater than <see cref="Price"/> the product is on sale.</summary>
    public decimal? OriginalPrice { get; set; }
    public string ImageUrl { get; set; } = "images/product-placeholder.svg";
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<Review> Reviews { get; set; } = [];

    public bool IsAvailable => Stock > 0;
    public bool IsOnSale => OriginalPrice is { } original && original > Price;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(ImageUrl);

        if (Price < 0)
        {
            throw new ShopException("Product price cannot be negative.");
        }

        if (OriginalPrice is < 0)
        {
            throw new ShopException("Original price cannot be negative.");
        }

        if (Stock < 0)
        {
            throw new ShopException("Product stock cannot be negative.");
        }
    }
}

public sealed class Review
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WishlistItem
{
    public int UserId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    public DateTimeOffset AddedAt { get; set; }
}

public sealed class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? UserId { get; set; }
    public string? CouponCode { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<CartItem> Items { get; set; } = [];
}

public sealed class CartItem
{
    public Guid CartId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public sealed class Coupon
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CouponType Type { get; set; }

    /// <summary>Percent (0-100) for <see cref="CouponType.Percentage"/>, an amount for <see cref="CouponType.FixedAmount"/>.</summary>
    public decimal Value { get; set; }
    public decimal MinSubtotal { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int? MaxRedemptions { get; set; }
    public int Redemptions { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Order
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public ShippingMethod ShippingMethod { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string? CouponCode { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
    public List<OrderStatusEntry> History { get; set; } = [];
}

public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class OrderStatusEntry
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset At { get; set; }
    public string? Note { get; set; }
}
