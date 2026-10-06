using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface IOrderStore
{
    IReadOnlyList<Order> GetByAccount(string accountEmail);
    IReadOnlyList<Order> GetAll();
    void Add(Order order);
}
