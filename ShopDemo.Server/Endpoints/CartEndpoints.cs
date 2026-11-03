using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this RouteGroupBuilder api)
    {
        var cart = api.MapGroup("/cart");

        cart.MapGet("", async (HttpContext context, CartService carts, ShippingMethod? shipping, CancellationToken ct) =>
            Results.Ok(await carts.GetAsync(context.CartOwner(), shipping ?? ShippingMethod.Standard, ct)));

        cart.MapPost("/items", async (AddCartItemRequest request, HttpContext context, CartService carts, CancellationToken ct) =>
        {
            RejectManagers(context);
            return Results.Ok(await carts.AddAsync(context.CartOwnerForWrite(), request.ProductId, request.Quantity, ct));
        });

        cart.MapPut("/items/{productId}", async (string productId, SetCartQuantityRequest request, HttpContext context, CartService carts, CancellationToken ct) =>
        {
            RejectManagers(context);
            return Results.Ok(await carts.SetQuantityAsync(context.CartOwnerForWrite(), productId, request.Quantity, ct));
        });

        cart.MapDelete("/items/{productId}", async (string productId, HttpContext context, CartService carts, CancellationToken ct) =>
            Results.Ok(await carts.RemoveAsync(context.CartOwnerForWrite(), productId, ct)));

        cart.MapDelete("", async (HttpContext context, CartService carts, CancellationToken ct) =>
            Results.Ok(await carts.ClearAsync(context.CartOwner(), ct)));

        cart.MapPost("/coupon", async (ApplyCouponRequest request, HttpContext context, CartService carts, CancellationToken ct) =>
            Results.Ok(await carts.ApplyCouponAsync(context.CartOwner(), request.Code, ct)));

        cart.MapDelete("/coupon", async (HttpContext context, CartService carts, CancellationToken ct) =>
            Results.Ok(await carts.RemoveCouponAsync(context.CartOwner(), ct)));
    }

    private static void RejectManagers(HttpContext context)
    {
        if (context.User.IsInRole(nameof(UserRole.StoreManager)))
        {
            throw ShopException.Forbidden("Store managers cannot shop. Sign in as a shopper to use the cart.");
        }
    }
}
