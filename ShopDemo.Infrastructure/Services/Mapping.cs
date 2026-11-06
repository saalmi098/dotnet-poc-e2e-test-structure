using ShopDemo.Contracts;
using ShopDemo.Contracts.Orders;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure.Services;

internal static class Mapping
{
    public static UserDto ToDto(this User user) => new(
        user.Id, user.Email, user.DisplayName, user.Role,
        user.Phone, user.AddressLine, user.PostalCode, user.City, user.Country);

    public static AdminProductDto ToAdminDto(this Product product) => new(
        product.Id, product.Name, product.Description, product.Category.Name, product.Price,
        product.OriginalPrice, product.ImageUrl, product.Stock, product.IsActive);

    public static OrderDto ToDto(this Order order, User viewer)
    {
        var isManager = viewer.Role == UserRole.StoreManager;
        return new OrderDto(
            order.Number,
            order.CreatedAt,
            order.Status,
            order.User?.Email ?? string.Empty,
            order.CustomerName,
            order.CustomerEmail,
            order.AddressLine,
            order.PostalCode,
            order.City,
            order.Country,
            order.ShippingMethod,
            order.PaymentMethod,
            order.PaymentReference,
            order.CouponCode,
            order.Subtotal,
            order.Discount,
            order.ShippingCost,
            order.Total,
            [.. order.Lines.Select(line => new OrderLineDto(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity))],
            [.. order.History.OrderBy(entry => entry.At).ThenBy(entry => entry.Id)
                .Select(entry => new OrderStatusEntryDto(entry.Status, entry.At, entry.Note))],
            isManager ? OrderRules.NextStatuses(order.Status) : [],
            !isManager && order.UserId == viewer.Id && OrderRules.CanShopperCancel(order.Status));
    }

    public static CouponDto ToDto(this Coupon coupon) => new(
        coupon.Code, coupon.Description, coupon.Type, coupon.Value, coupon.MinSubtotal,
        coupon.ExpiresAt, coupon.MaxRedemptions, coupon.Redemptions, coupon.IsActive);
}
