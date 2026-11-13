using ShopDemo.Domain;

namespace ShopDemo.Contracts.Orders;

public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? Note = null);
