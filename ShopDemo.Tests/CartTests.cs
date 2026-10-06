using ShopDemo.Domain;
using Xunit;

namespace ShopDemo.Tests;

public sealed class CartTests
{
    [Fact]
    public void Add_tracks_quantity_and_subtotal()
    {
        var cart = new Cart();
        var product = CreateProduct(price: 12.50m, stock: 5);

        cart.Add(product);
        cart.Add(product, 2);

        Assert.Equal(3, cart.ItemCount);
        Assert.Equal(37.50m, cart.Subtotal);
        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void Add_rejects_quantity_above_stock()
    {
        var cart = new Cart();
        var product = CreateProduct(stock: 2);

        cart.Add(product, 2);

        Assert.Throws<InvalidOperationException>(() => cart.Add(product));
    }

    [Fact]
    public void Add_rejects_non_positive_quantity()
    {
        var cart = new Cart();

        Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(CreateProduct(), 0));
    }

    [Fact]
    public void Set_quantity_zero_removes_item_and_negative_is_rejected()
    {
        var cart = new Cart();
        var product = CreateProduct();
        cart.Add(product);

        Assert.Throws<ArgumentOutOfRangeException>(() => cart.SetQuantity(product, -1));
        cart.SetQuantity(product, 0);

        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
    }

    [Fact]
    public void Add_rejects_out_of_stock_product()
    {
        var cart = new Cart();

        Assert.Throws<InvalidOperationException>(() => cart.Add(CreateProduct(stock: 0)));
    }

    private static Product CreateProduct(decimal price = 10m, int stock = 5) => new()
    {
        Id = "test-product",
        Name = "Test product",
        Description = "A test item.",
        Category = "Test",
        Price = price,
        ImageUrl = "images/product-placeholder.svg",
        Stock = stock
    };
}
