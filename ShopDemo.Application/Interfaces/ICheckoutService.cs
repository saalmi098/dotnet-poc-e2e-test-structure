using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface ICheckoutService
{
    IReadOnlyList<Order> GetOrdersForCurrentUser();
    Order PlaceOrder(CheckoutDetails details);
}
