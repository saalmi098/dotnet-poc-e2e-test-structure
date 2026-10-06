using ShopDemo.Application.Interfaces;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure;

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly List<Order> _orders = [];

    public IReadOnlyList<Order> GetByAccount(string accountEmail) =>
        [.. _orders
            .Where(order => string.Equals(order.AccountEmail, accountEmail, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(order => order.CreatedAt)];

    public IReadOnlyList<Order> GetAll() =>
        [.. _orders.OrderByDescending(order => order.CreatedAt)];

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _orders.Add(order);
    }
}
