using ShopDemo.Domain;

namespace ShopDemo.Contracts.Orders;

public sealed record OrderStatusEntryDto(OrderStatus Status, DateTimeOffset At, string? Note);
