using System.Security.Claims;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Server.Endpoints;

public static class Policies
{
    public const string Manager = "Manager";
}

internal static class HttpContextExtensions
{
    public const string GuestCookieName = "shopdemo.guest";

    public static int? UserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static int RequireUserId(this ClaimsPrincipal principal) =>
        principal.UserId() ?? throw ShopException.Unauthorized("Log in to continue.");

    /// <summary>The cart owner for reads: the signed-in account, else the guest cookie (if any).</summary>
    public static CartOwner CartOwner(this HttpContext context)
    {
        if (context.User.UserId() is { } userId)
        {
            return Infrastructure.Services.CartOwner.ForUser(userId);
        }

        return Guid.TryParse(context.Request.Cookies[GuestCookieName], out var guestId)
            ? Infrastructure.Services.CartOwner.ForGuest(guestId)
            : default;
    }

    /// <summary>The cart owner for writes: creates the guest cookie when an anonymous visitor has none yet.</summary>
    public static CartOwner CartOwnerForWrite(this HttpContext context)
    {
        var owner = context.CartOwner();
        if (owner.UserId is not null || owner.GuestId is not null)
        {
            return owner;
        }

        var guestId = Guid.NewGuid();
        context.Response.Cookies.Append(GuestCookieName, guestId.ToString(), GuestCookieOptions(context));
        return Infrastructure.Services.CartOwner.ForGuest(guestId);
    }

    public static CookieOptions GuestCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        MaxAge = TimeSpan.FromDays(30),
        IsEssential = true
    };
}
