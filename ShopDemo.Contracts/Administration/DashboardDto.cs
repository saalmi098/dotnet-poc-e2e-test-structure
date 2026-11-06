namespace ShopDemo.Contracts;

public sealed record DashboardDto(
    int TotalOrders,
    int OpenOrders,
    decimal Revenue,
    int ProductCount,
    int LowStockCount,
    int OutOfStockCount,
    int CustomerCount,
    IReadOnlyList<AdminProductDto> LowStockProducts);
