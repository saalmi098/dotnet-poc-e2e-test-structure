using ShopDemo.Contracts;
using ShopDemo.Infrastructure.Data;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this RouteGroupBuilder api)
    {
        var admin = api.MapGroup("/admin").RequireAuthorization(Policies.Manager);

        admin.MapGet("/dashboard", async (AdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetDashboardAsync(ct)));

        admin.MapGet("/products", async (AdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetProductsAsync(ct)));

        admin.MapGet("/products/{id}", async (string id, AdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetProductAsync(id, ct)));

        admin.MapPost("/products", async (SaveProductRequest request, AdminService service, CancellationToken ct) =>
        {
            var product = await service.CreateProductAsync(request, ct);
            return Results.Created($"/api/admin/products/{product.Id}", product);
        });

        admin.MapPut("/products/{id}", async (string id, SaveProductRequest request, AdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateProductAsync(id, request, ct)));

        admin.MapDelete("/products/{id}", async (string id, AdminService service, CancellationToken ct) =>
            Results.Ok(new { deleted = await service.DeleteProductAsync(id, ct) }));

        admin.MapPut("/products/{id}/stock", async (string id, UpdateStockRequest request, AdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateStockAsync(id, request.Stock, ct)));

        admin.MapGet("/coupons", async (AdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetCouponsAsync(ct)));

        admin.MapPost("/coupons", async (SaveCouponRequest request, AdminService service, CancellationToken ct) =>
            Results.Ok(await service.SaveCouponAsync(request, create: true, ct)));

        admin.MapPut("/coupons/{code}", async (string code, SaveCouponRequest request, AdminService service, CancellationToken ct) =>
            Results.Ok(await service.SaveCouponAsync(request with { Code = code }, create: false, ct)));

        admin.MapDelete("/coupons/{code}", async (string code, AdminService service, CancellationToken ct) =>
        {
            await service.DeleteCouponAsync(code, ct);
            return Results.NoContent();
        });
    }

    /// <summary>Resets the database to its seed data so end-to-end tests can start from a known state.</summary>
    public static void MapTestSupportEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/test/reset", async (DatabaseSeeder seeder, CancellationToken ct) =>
        {
            await seeder.ResetAsync(ct);
            return Results.NoContent();
        });
    }
}
