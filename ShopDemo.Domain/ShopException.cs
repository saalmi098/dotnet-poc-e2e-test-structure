namespace ShopDemo.Domain;

/// <summary>A business-rule failure that is safe to show to the user. <see cref="StatusCode"/> is the HTTP status the API reports.</summary>
public sealed class ShopException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ShopException NotFound(string message) => new(message, 404);
    public static ShopException Forbidden(string message) => new(message, 403);
    public static ShopException Unauthorized(string message) => new(message, 401);
    public static ShopException Conflict(string message) => new(message, 409);
}
