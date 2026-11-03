using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ShopDemo.Contracts;
using ShopDemo.Domain;

namespace ShopDemo.Client.Services;

/// <summary>An API call failed; <see cref="Exception.Message"/> is safe to show to the user.</summary>
public sealed class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>Typed wrapper around the ShopDemo.Server REST API.</summary>
public sealed class ShopApi(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ---- Accounts ---------------------------------------------------------------------------

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/auth/me", ct);
        await EnsureSuccessAsync(response);
        return response.StatusCode == HttpStatusCode.NoContent
            ? null
            : await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions, ct);
    }

    public Task<UserDto> LoginAsync(string email, string password, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/login", new LoginRequest(email, password), ct);

    public Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", request, ct);

    public Task LogoutAsync(CancellationToken ct = default) => SendAsync(HttpMethod.Post, "api/auth/logout", null, ct);

    public Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/auth/profile", request, ct);

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/auth/change-password", request, ct);

    // ---- Catalog ----------------------------------------------------------------------------

    public Task<PagedResult<ProductSummaryDto>> GetProductsAsync(ProductQuery query, CancellationToken ct = default)
    {
        var parameters = new List<string>
        {
            $"sort={query.Sort}",
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };
        if (!string.IsNullOrWhiteSpace(query.Search)) parameters.Add($"q={Uri.EscapeDataString(query.Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(query.Category)) parameters.Add($"category={Uri.EscapeDataString(query.Category)}");
        if (query.MinPrice is { } min) parameters.Add($"minPrice={min.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (query.MaxPrice is { } max) parameters.Add($"maxPrice={max.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (query.InStockOnly) parameters.Add("inStock=true");
        if (query.OnSaleOnly) parameters.Add("onSale=true");
        return SendAsync<PagedResult<ProductSummaryDto>>(HttpMethod.Get, $"api/products?{string.Join('&', parameters)}", null, ct);
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CategoryDto>>(HttpMethod.Get, "api/categories", null, ct);

    public Task<ProductDetailDto> GetProductAsync(string productId, CancellationToken ct = default) =>
        SendAsync<ProductDetailDto>(HttpMethod.Get, $"api/products/{Uri.EscapeDataString(productId)}", null, ct);

    public Task<IReadOnlyList<ReviewDto>> GetReviewsAsync(string productId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ReviewDto>>(HttpMethod.Get, $"api/products/{Uri.EscapeDataString(productId)}/reviews", null, ct);

    public Task<ReviewDto> AddReviewAsync(string productId, CreateReviewRequest request, CancellationToken ct = default) =>
        SendAsync<ReviewDto>(HttpMethod.Post, $"api/products/{Uri.EscapeDataString(productId)}/reviews", request, ct);

    public Task DeleteReviewAsync(int reviewId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/reviews/{reviewId}", null, ct);

    // ---- Wishlist ---------------------------------------------------------------------------

    public Task<IReadOnlyList<ProductSummaryDto>> GetWishlistAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProductSummaryDto>>(HttpMethod.Get, "api/wishlist", null, ct);

    public Task AddToWishlistAsync(string productId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/wishlist/{Uri.EscapeDataString(productId)}", null, ct);

    public Task RemoveFromWishlistAsync(string productId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/wishlist/{Uri.EscapeDataString(productId)}", null, ct);

    // ---- Cart -------------------------------------------------------------------------------

    public Task<CartDto> GetCartAsync(ShippingMethod shipping = ShippingMethod.Standard, CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Get, $"api/cart?shipping={shipping}", null, ct);

    public Task<CartDto> AddToCartAsync(string productId, int quantity, CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Post, "api/cart/items", new AddCartItemRequest(productId, quantity), ct);

    public Task<CartDto> SetCartQuantityAsync(string productId, int quantity, CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Put, $"api/cart/items/{Uri.EscapeDataString(productId)}", new SetCartQuantityRequest(quantity), ct);

    public Task<CartDto> RemoveFromCartAsync(string productId, CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Delete, $"api/cart/items/{Uri.EscapeDataString(productId)}", null, ct);

    public Task<CartDto> ApplyCouponAsync(string code, CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Post, "api/cart/coupon", new ApplyCouponRequest(code), ct);

    public Task<CartDto> RemoveCouponAsync(CancellationToken ct = default) =>
        SendAsync<CartDto>(HttpMethod.Delete, "api/cart/coupon", null, ct);

    // ---- Orders -----------------------------------------------------------------------------

    public Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Post, "api/orders", request, ct);

    public Task<PagedResult<OrderDto>> GetOrdersAsync(OrderStatus? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var parameters = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is { } filter) parameters.Add($"status={filter}");
        if (!string.IsNullOrWhiteSpace(search)) parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");
        return SendAsync<PagedResult<OrderDto>>(HttpMethod.Get, $"api/orders?{string.Join('&', parameters)}", null, ct);
    }

    public Task<OrderDto> GetOrderAsync(string number, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Get, $"api/orders/{Uri.EscapeDataString(number)}", null, ct);

    public Task<OrderDto> CancelOrderAsync(string number, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Post, $"api/orders/{Uri.EscapeDataString(number)}/cancel", null, ct);

    public Task<OrderDto> UpdateOrderStatusAsync(string number, OrderStatus status, string? note, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Put, $"api/orders/{Uri.EscapeDataString(number)}/status", new UpdateOrderStatusRequest(status, note), ct);

    // ---- Administration ---------------------------------------------------------------------

    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) =>
        SendAsync<DashboardDto>(HttpMethod.Get, "api/admin/dashboard", null, ct);

    public Task<IReadOnlyList<AdminProductDto>> GetAdminProductsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<AdminProductDto>>(HttpMethod.Get, "api/admin/products", null, ct);

    public Task<AdminProductDto> GetAdminProductAsync(string productId, CancellationToken ct = default) =>
        SendAsync<AdminProductDto>(HttpMethod.Get, $"api/admin/products/{Uri.EscapeDataString(productId)}", null, ct);

    public Task<AdminProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken ct = default) =>
        SendAsync<AdminProductDto>(HttpMethod.Post, "api/admin/products", request, ct);

    public Task<AdminProductDto> UpdateProductAsync(string productId, SaveProductRequest request, CancellationToken ct = default) =>
        SendAsync<AdminProductDto>(HttpMethod.Put, $"api/admin/products/{Uri.EscapeDataString(productId)}", request, ct);

    public async Task<bool> DeleteProductAsync(string productId, CancellationToken ct = default)
    {
        var result = await SendAsync<DeleteProductResult>(HttpMethod.Delete, $"api/admin/products/{Uri.EscapeDataString(productId)}", null, ct);
        return result.Deleted;
    }

    public Task<AdminProductDto> UpdateStockAsync(string productId, int stock, CancellationToken ct = default) =>
        SendAsync<AdminProductDto>(HttpMethod.Put, $"api/admin/products/{Uri.EscapeDataString(productId)}/stock", new UpdateStockRequest(stock), ct);

    public Task<IReadOnlyList<CouponDto>> GetCouponsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CouponDto>>(HttpMethod.Get, "api/admin/coupons", null, ct);

    public Task<CouponDto> CreateCouponAsync(SaveCouponRequest request, CancellationToken ct = default) =>
        SendAsync<CouponDto>(HttpMethod.Post, "api/admin/coupons", request, ct);

    public Task<CouponDto> UpdateCouponAsync(SaveCouponRequest request, CancellationToken ct = default) =>
        SendAsync<CouponDto>(HttpMethod.Put, $"api/admin/coupons/{Uri.EscapeDataString(request.Code)}", request, ct);

    public Task DeleteCouponAsync(string code, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/coupons/{Uri.EscapeDataString(code)}", null, ct);

    // ---- Plumbing ---------------------------------------------------------------------------

    private sealed record DeleteProductResult(bool Deleted);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, url, body, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
            ?? throw new ApiException("The server returned an empty response.", response.StatusCode);
    }

    private async Task SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, url, body, ct);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException($"The server could not be reached. {exception.Message}", HttpStatusCode.ServiceUnavailable);
        }

        try
        {
            await EnsureSuccessAsync(response);
        }
        catch
        {
            response.Dispose();
            throw;
        }

        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? detail = null;
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (document.RootElement.TryGetProperty("detail", out var element))
            {
                detail = element.GetString();
            }
        }
        catch (JsonException)
        {
            // Not a problem-details body; fall back to the status code.
        }

        throw new ApiException(detail ?? $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).", response.StatusCode);
    }
}
