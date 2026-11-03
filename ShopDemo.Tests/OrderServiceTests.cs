using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;
using Xunit;

namespace ShopDemo.Tests;

public sealed class OrderServiceTests : IDisposable
{
    private readonly TestShop _shop = new();

    public void Dispose() => _shop.Dispose();

    private CartOwner Demo => CartOwner.ForUser(_shop.DemoShopperId);

    private static PlaceOrderRequest Request(
        PaymentMethod payment = PaymentMethod.Invoice,
        ShippingMethod shipping = ShippingMethod.Standard,
        string? card = null) => new(
            "Demo Shopper", "demo@test.com", "Demo Street 1", "1010", "Vienna", "Austria",
            shipping, payment, card, "Demo Shopper", "12/40", "123");

    [Fact]
    public async Task Placing_an_order_decrements_stock_and_clears_the_cart()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug", 2);

        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        Assert.StartsWith("ORD-", order.Number);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(48m + Pricing.StandardShippingCost - 0m, order.Total);
        Assert.Equal(10, _shop.StockOf("ceramic-mug"));
        Assert.Empty((await _shop.Carts.GetAsync(Demo)).Items);
        Assert.Equal("demo@test.com", order.AccountEmail);
    }

    [Fact]
    public async Task Order_keeps_the_prices_of_the_time_of_purchase()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");
        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        await _shop.Admin.UpdateProductAsync("ceramic-mug",
            new SaveProductRequest("ceramic-mug", "Everyday Ceramic Mug", "A mug.", "Home", 99m, null, null, 5));

        var loaded = await _shop.Orders.GetAsync(_shop.DemoShopperId, order.Number);
        Assert.Equal(24m, loaded.Lines.Single().UnitPrice);
    }

    [Fact]
    public async Task Coupon_discount_and_redemption_are_recorded()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp", 2);
        await _shop.Carts.ApplyCouponAsync(Demo, "WELCOME10");

        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        Assert.Equal("WELCOME10", order.CouponCode);
        Assert.Equal(9.2m, order.Discount);
        Assert.Equal(1, _shop.Db.Coupons.Single(coupon => coupon.Code == "WELCOME10").Redemptions);
    }

    [Fact]
    public async Task Empty_cart_cannot_be_ordered()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request()));

        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public async Task Store_managers_cannot_place_orders()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.PlaceOrderAsync(_shop.ManagerId, Request()));

        Assert.Equal(403, exception.StatusCode);
    }

    [Theory]
    [InlineData("", "Enter your name.")]
    public async Task Missing_contact_details_are_rejected(string name, string expected)
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        var exception = await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request() with { CustomerName = name }));

        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task Invalid_email_is_rejected()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request() with { Email = "not-an-email" }));
    }

    [Fact]
    public async Task Stock_taken_by_someone_else_blocks_the_order_and_changes_nothing()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp", 3);
        await _shop.Admin.UpdateStockAsync("desk-lamp", 2);

        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request()));

        Assert.Contains("Only 2", exception.Message);
        Assert.Equal(2, _shop.StockOf("desk-lamp"));
        Assert.Single((await _shop.Carts.GetAsync(Demo)).Items);
    }

    [Fact]
    public async Task Credit_card_payment_stores_only_the_last_four_digits()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId,
            Request(PaymentMethod.CreditCard, card: "4242 4242 4242 4242"));

        Assert.Equal("Visa •••• 4242", order.PaymentReference);
    }

    [Fact]
    public async Task Declined_card_leaves_stock_and_cart_untouched()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId,
            Request(PaymentMethod.CreditCard, card: OrderService.DeclinedCardNumber)));

        Assert.Equal(402, exception.StatusCode);
        Assert.Equal(12, _shop.StockOf("ceramic-mug"));
        Assert.Single((await _shop.Carts.GetAsync(Demo)).Items);
    }

    [Fact]
    public async Task Invalid_card_number_is_rejected()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId,
            Request(PaymentMethod.CreditCard, card: "1234 5678 9012 3456")));

        Assert.Contains("card number", exception.Message);
    }

    [Fact]
    public async Task Shopper_sees_only_their_own_orders_while_manager_sees_all()
    {
        var shopperOrders = await _shop.Orders.ListAsync(_shop.JaneId, null, null, 1, 50);
        var managerOrders = await _shop.Orders.ListAsync(_shop.ManagerId, null, null, 1, 50);

        Assert.All(shopperOrders.Items, order => Assert.Equal("jane@test.com", order.AccountEmail));
        Assert.Equal(3, managerOrders.TotalCount);
        Assert.Equal(2, shopperOrders.TotalCount);
    }

    [Fact]
    public async Task Another_shoppers_order_is_reported_as_not_found()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.GetAsync(_shop.DemoShopperId, "ORD-SEED0001"));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task Orders_can_be_filtered_by_status_and_searched()
    {
        var delivered = await _shop.Orders.ListAsync(_shop.ManagerId, OrderStatus.Delivered, null, 1, 50);
        var searched = await _shop.Orders.ListAsync(_shop.ManagerId, null, "seed0002", 1, 50);

        Assert.Equal(2, delivered.TotalCount);
        Assert.Equal("ORD-SEED0002", Assert.Single(searched.Items).Number);
    }

    [Fact]
    public async Task Shopper_can_cancel_a_pending_order_and_the_stock_returns()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug", 2);
        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        var cancelled = await _shop.Orders.CancelAsync(_shop.DemoShopperId, order.Number);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(12, _shop.StockOf("ceramic-mug"));
        Assert.Contains(cancelled.History, entry => entry.Status == OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancelling_gives_the_coupon_redemption_back()
    {
        await _shop.Carts.AddAsync(Demo, "desk-lamp");
        await _shop.Carts.ApplyCouponAsync(Demo, "WELCOME10");
        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        await _shop.Orders.CancelAsync(_shop.DemoShopperId, order.Number);

        Assert.Equal(0, _shop.Db.Coupons.AsNoTracking().Single(coupon => coupon.Code == "WELCOME10").Redemptions);
    }

    [Fact]
    public async Task Shipped_orders_cannot_be_cancelled_by_the_shopper()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Orders.CancelAsync(_shop.JaneId, "ORD-SEED0002"));

        Assert.Contains("shipped", exception.Message);
    }

    [Fact]
    public async Task Manager_moves_an_order_through_the_fulfilment_flow()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");
        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        var processing = await _shop.Orders.UpdateStatusAsync(_shop.ManagerId, order.Number, OrderStatus.Processing, null);
        var shipped = await _shop.Orders.UpdateStatusAsync(_shop.ManagerId, order.Number, OrderStatus.Shipped, "DHL 123");
        var delivered = await _shop.Orders.UpdateStatusAsync(_shop.ManagerId, order.Number, OrderStatus.Delivered, null);

        Assert.Equal(OrderStatus.Processing, processing.Status);
        Assert.Equal("DHL 123", shipped.History.Last().Note);
        Assert.Equal(OrderStatus.Delivered, delivered.Status);
        Assert.Empty(delivered.AllowedNextStatuses);
        Assert.Equal(4, delivered.History.Count);
    }

    [Fact]
    public async Task Invalid_status_transitions_are_rejected()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");
        var order = await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId, Request());

        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Orders.UpdateStatusAsync(_shop.ManagerId, order.Number, OrderStatus.Delivered, null));
    }

    [Fact]
    public async Task Shoppers_cannot_change_the_order_status()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Orders.UpdateStatusAsync(_shop.JaneId, "ORD-SEED0002", OrderStatus.Delivered, null));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task Saving_the_address_updates_the_profile()
    {
        await _shop.Carts.AddAsync(Demo, "ceramic-mug");

        await _shop.Orders.PlaceOrderAsync(_shop.DemoShopperId,
            Request() with { AddressLine = "New Street 5", City = "Linz", SaveAddress = true });

        var user = await _shop.Accounts.FindAsync(_shop.DemoShopperId);
        Assert.Equal("New Street 5", user!.AddressLine);
        Assert.Equal("Linz", user.City);
    }
}
