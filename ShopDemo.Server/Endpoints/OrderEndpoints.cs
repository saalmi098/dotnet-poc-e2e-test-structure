using ShopDemo.Contracts.Orders;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this RouteGroupBuilder api)
    {
        var orders = api.MapGroup("/orders").RequireAuthorization();

        orders.MapPost("", async (PlaceOrderRequest request, HttpContext context, OrderService service, CancellationToken ct) =>
            Results.Ok(await service.PlaceOrderAsync(context.User.RequireUserId(), request, ct)));

        orders.MapGet("", async (HttpContext context, OrderService service, OrderStatus? status, string? search, int? page, int? pageSize, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(context.User.RequireUserId(), status, search, page ?? 1, pageSize ?? 20, ct)));

        orders.MapGet("/{number}", async (string number, HttpContext context, OrderService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(context.User.RequireUserId(), number, ct)));

        orders.MapPost("/{number}/cancel", async (string number, HttpContext context, OrderService service, CancellationToken ct) =>
            Results.Ok(await service.CancelAsync(context.User.RequireUserId(), number, ct)));

        orders.MapPut("/{number}/status", async (string number, UpdateOrderStatusRequest request, HttpContext context, OrderService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateStatusAsync(context.User.RequireUserId(), number, request.Status, request.Note, ct)))
            .RequireAuthorization(Policies.Manager);
    }
}
