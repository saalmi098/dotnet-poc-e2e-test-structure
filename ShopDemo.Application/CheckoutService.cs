using ShopDemo.Application.Interfaces;
using ShopDemo.Domain;

namespace ShopDemo.Application;

public sealed class CheckoutService(
    IDemoAuthService authService,
    ICartService cartService,
    IOrderStore orderStore) : ICheckoutService
{
    public IReadOnlyList<Order> GetOrdersForCurrentUser()
    {
        var user = authService.CurrentUser
            ?? throw new InvalidOperationException("Log in to view order history.");

        return user.Role switch
        {
            DemoRole.Shopper => orderStore.GetByAccount(user.Email),
            DemoRole.StoreManager => orderStore.GetAll(),
            _ => throw new UnauthorizedAccessException("This account cannot view order history.")
        };
    }

    public Order PlaceOrder(CheckoutDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var user = authService.CurrentUser
            ?? throw new InvalidOperationException("Log in before checking out.");

        if (user.Role != DemoRole.Shopper)
        {
            throw new UnauthorizedAccessException("Store managers cannot place customer orders.");
        }

        details.Validate();

        if (cartService.Items.Count == 0)
        {
            throw new InvalidOperationException("Your cart is empty.");
        }

        var lines = cartService.Items
            .Select(item => new OrderLine(
                item.Product.Id,
                item.Product.Name,
                item.Product.Price,
                item.Quantity))
            .ToArray();
        var order = new Order(
            $"ORD-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            DateTimeOffset.UtcNow,
            user.Email,
            details.CustomerName.Trim(),
            details.Email.Trim(),
            lines);

        orderStore.Add(order);
        cartService.Clear();
        return order;
    }
}
