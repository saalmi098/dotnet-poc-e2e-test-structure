using ShopDemo.Contracts.Catalog;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/categories", async (CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetCategoriesAsync(ct)));

        api.MapGet("/products", async (
            HttpContext context,
            CatalogService catalog,
            string? q,
            string? category,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            bool? onSale,
            ProductSort? sort,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var query = new ProductQuery(q, category, minPrice, maxPrice, inStock ?? false, onSale ?? false,
                sort ?? ProductSort.Featured, page ?? 1, pageSize ?? 12);
            return Results.Ok(await catalog.SearchAsync(query, context.User.UserId(), ct));
        });

        api.MapGet("/products/{id}", async (string id, HttpContext context, CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetProductAsync(id, context.User.UserId(), ct)));

        api.MapGet("/products/{id}/reviews", async (string id, CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetReviewsAsync(id, ct)));

        api.MapPost("/products/{id}/reviews", async (string id, CreateReviewRequest request, HttpContext context, CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.AddReviewAsync(context.User.RequireUserId(), id, request, ct))).RequireAuthorization();

        api.MapDelete("/reviews/{id:int}", async (int id, HttpContext context, CatalogService catalog, CancellationToken ct) =>
        {
            await catalog.DeleteReviewAsync(id, context.User.RequireUserId(), ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    public static void MapWishlistEndpoints(this RouteGroupBuilder api)
    {
        var wishlist = api.MapGroup("/wishlist").RequireAuthorization();

        wishlist.MapGet("", async (HttpContext context, WishlistService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(context.User.RequireUserId(), ct)));

        wishlist.MapPut("/{productId}", async (string productId, HttpContext context, WishlistService service, CancellationToken ct) =>
        {
            await service.AddAsync(context.User.RequireUserId(), productId, ct);
            return Results.NoContent();
        });

        wishlist.MapDelete("/{productId}", async (string productId, HttpContext context, WishlistService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(context.User.RequireUserId(), productId, ct);
            return Results.NoContent();
        });
    }
}
