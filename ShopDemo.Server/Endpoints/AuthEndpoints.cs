using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth");

        auth.MapPost("/register", async (RegisterRequest request, HttpContext context, AccountService accounts, CartService carts, CancellationToken ct) =>
        {
            var user = await accounts.RegisterAsync(request, ct);
            await SignInAsync(context, user, carts, ct);
            return Results.Ok(user);
        });

        auth.MapPost("/login", async (LoginRequest request, HttpContext context, AccountService accounts, CartService carts, CancellationToken ct) =>
        {
            var user = await accounts.AuthenticateAsync(request.Email, request.Password, ct)
                ?? throw ShopException.Unauthorized("The email or password is incorrect.");
            await SignInAsync(context, user, carts, ct);
            return Results.Ok(user);
        });

        auth.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        auth.MapGet("/me", async (HttpContext context, AccountService accounts, CancellationToken ct) =>
        {
            if (context.User.UserId() is not { } userId)
            {
                return Results.NoContent();
            }

            var user = await accounts.FindAsync(userId, ct);
            if (user is null)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.NoContent();
            }

            return Results.Ok(user);
        });

        auth.MapPut("/profile", async (UpdateProfileRequest request, HttpContext context, AccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.UpdateProfileAsync(context.User.RequireUserId(), request, ct))).RequireAuthorization();

        auth.MapPost("/change-password", async (ChangePasswordRequest request, HttpContext context, AccountService accounts, CancellationToken ct) =>
        {
            await accounts.ChangePasswordAsync(context.User.RequireUserId(), request, ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static async Task SignInAsync(HttpContext context, UserDto user, CartService carts, CancellationToken ct)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        // Carry the anonymous cart over to the account.
        if (Guid.TryParse(context.Request.Cookies[HttpContextExtensions.GuestCookieName], out var guestId))
        {
            if (user.Role == UserRole.Shopper)
            {
                await carts.MergeGuestCartAsync(guestId, user.Id, ct);
            }

            context.Response.Cookies.Delete(HttpContextExtensions.GuestCookieName);
        }
    }
}
