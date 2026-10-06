using ShopDemo.Application;
using ShopDemo.Application.Interfaces;
using ShopDemo.Domain;
using Xunit;

namespace ShopDemo.Tests;

public sealed class CheckoutServiceTests
{
    [Fact]
    public void Place_order_creates_order_and_clears_cart()
    {
        var auth = new FakeAuthService { CurrentUser = new DemoUser("demo@test.com", "Demo Shopper") };
        var cart = new FakeCartService(
        [
            new CartLine(new Product
            {
                Id = "mug",
                Name = "Ceramic mug",
                Description = "A mug.",
                Category = "Home",
                Price = 24m,
                ImageUrl = "images/product-placeholder.svg",
                Stock = 5
            }, 2)
        ]);
        var store = new FakeOrderStore();
        var service = new CheckoutService(auth, cart, store);

        var order = service.PlaceOrder(new CheckoutDetails("Shopper", "shopper@example.com"));

        Assert.StartsWith("ORD-", order.Number);
        Assert.Equal(48m, order.Total);
        Assert.Equal("demo@test.com", order.AccountEmail);
        Assert.Empty(cart.Items);
        Assert.Same(order, Assert.Single(store.Orders));
    }

    [Fact]
    public void Place_order_rejects_unauthenticated_user()
    {
        var service = new CheckoutService(new FakeAuthService(), new FakeCartService([]), new FakeOrderStore());

        Assert.Throws<InvalidOperationException>(() =>
            service.PlaceOrder(new CheckoutDetails("Shopper", "shopper@example.com")));
    }

    [Fact]
    public void Place_order_rejects_empty_cart()
    {
        var auth = new FakeAuthService { CurrentUser = new DemoUser("demo@test.com", "Demo Shopper") };
        var service = new CheckoutService(auth, new FakeCartService([]), new FakeOrderStore());

        Assert.Throws<InvalidOperationException>(() =>
            service.PlaceOrder(new CheckoutDetails("Shopper", "shopper@example.com")));
    }

    [Fact]
    public void Place_order_rejects_store_manager()
    {
        var auth = new FakeAuthService
        {
            CurrentUser = new DemoUser("admin@test.com", "Store Manager", DemoRole.StoreManager)
        };
        var service = new CheckoutService(auth, new FakeCartService([]), new FakeOrderStore());

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.PlaceOrder(new CheckoutDetails("Manager", "manager@example.com")));
    }

    [Fact]
    public void Store_manager_can_see_all_orders_created_in_the_session()
    {
        var store = new FakeOrderStore();
        store.Add(CreateOrder("shopper-one@test.com"));
        store.Add(CreateOrder("shopper-two@test.com"));
        var auth = new FakeAuthService
        {
            CurrentUser = new DemoUser("admin@test.com", "Store Manager", DemoRole.StoreManager)
        };
        var service = new CheckoutService(auth, new FakeCartService([]), store);

        Assert.Equal(2, service.GetOrdersForCurrentUser().Count);
    }

    [Fact]
    public void Shopper_only_sees_their_own_orders()
    {
        var store = new FakeOrderStore();
        store.Add(CreateOrder("demo@test.com"));
        store.Add(CreateOrder("another-shopper@test.com"));
        var auth = new FakeAuthService
        {
            CurrentUser = new DemoUser("demo@test.com", "Demo Shopper")
        };
        var service = new CheckoutService(auth, new FakeCartService([]), store);

        var order = Assert.Single(service.GetOrdersForCurrentUser());
        Assert.Equal("demo@test.com", order.AccountEmail);
    }

    private static Order CreateOrder(string accountEmail) => new(
        $"ORD-{Guid.NewGuid():N}"[..12],
        DateTimeOffset.UtcNow,
        accountEmail,
        "Shopper",
        "shopper@example.com",
        [new OrderLine("mug", "Mug", 10m, 1)]);

    private sealed class FakeAuthService : IDemoAuthService
    {
        public DemoUser? CurrentUser { get; set; }
        public event Action? AuthenticationStateChanged
        {
            add { }
            remove { }
        }

        public Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public void Logout() => CurrentUser = null;
    }

    private sealed class FakeCartService(IReadOnlyList<CartLine> items) : ICartService
    {
        private IReadOnlyList<CartLine> _items = items;

        public IReadOnlyList<CartLine> Items => _items;
        public int ItemCount => _items.Sum(item => item.Quantity);
        public decimal Subtotal => _items.Sum(item => item.LineTotal);
        public event Action? CartChanged
        {
            add { }
            remove { }
        }
        public Task AddProductAsync(string productId, int quantity = 1, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task SetQuantityAsync(string productId, int quantity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public void RemoveProduct(string productId) => throw new NotSupportedException();
        public void Clear() => _items = [];
    }

    private sealed class FakeOrderStore : IOrderStore
    {
        public List<Order> Orders { get; } = [];
        public IReadOnlyList<Order> GetByAccount(string accountEmail) =>
            [.. Orders.Where(order => order.AccountEmail == accountEmail)];
        public IReadOnlyList<Order> GetAll() => [.. Orders];
        public void Add(Order order) => Orders.Add(order);
    }
}
