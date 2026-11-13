using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

/// <summary>Store-manager operations. Authorization (manager role) is enforced by the API layer.</summary>
public sealed partial class AdminService(ShopDbContext db, TimeProvider clock)
{
    public const int LowStockThreshold = 5;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public async Task<IReadOnlyList<AdminProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await db.Products.AsNoTracking().Include(product => product.Category)
            .OrderBy(product => product.SortOrder).ThenBy(product => product.Name)
            .ToListAsync(cancellationToken);
        return [.. products.Select(product => product.ToAdminDto())];
    }

    public async Task<AdminProductDto> GetProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.AsNoTracking().Include(candidate => candidate.Category)
            .FirstOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw ShopException.NotFound($"Product '{productId}' was not found.");
        return product.ToAdminDto();
    }

    public async Task<AdminProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var id = (request.Id ?? string.Empty).Trim().ToLowerInvariant();
        if (!SlugPattern().IsMatch(id))
        {
            throw new ShopException("The product id may only contain lowercase letters, digits and hyphens.");
        }

        if (await db.Products.AnyAsync(product => product.Id == id, cancellationToken))
        {
            throw ShopException.Conflict($"A product with id '{id}' already exists.");
        }

        var product = new Product
        {
            Id = id,
            CreatedAt = clock.GetUtcNow(),
            SortOrder = (await db.Products.MaxAsync(candidate => (int?)candidate.SortOrder, cancellationToken) ?? 0) + 1
        };
        await ApplyAsync(product, request, cancellationToken);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product.ToAdminDto();
    }

    public async Task<AdminProductDto> UpdateProductAsync(string productId, SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw ShopException.NotFound($"Product '{productId}' was not found.");
        await ApplyAsync(product, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return product.ToAdminDto();
    }

    /// <summary>Deletes a product, or only deactivates it when orders reference it. Returns true when it was deleted.</summary>
    public async Task<bool> DeleteProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw ShopException.NotFound($"Product '{productId}' was not found.");

        if (await db.OrderLines.AnyAsync(line => line.ProductId == productId, cancellationToken))
        {
            product.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AdminProductDto> UpdateStockAsync(string productId, int stock, CancellationToken cancellationToken = default)
    {
        if (stock < 0)
        {
            throw new ShopException("Stock cannot be negative.");
        }

        var product = await db.Products.Include(candidate => candidate.Category)
            .FirstOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw ShopException.NotFound($"Product '{productId}' was not found.");
        product.Stock = stock;
        await db.SaveChangesAsync(cancellationToken);
        return product.ToAdminDto();
    }

    public async Task<IReadOnlyList<CouponDto>> GetCouponsAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await db.Coupons.AsNoTracking().OrderBy(coupon => coupon.Code).ToListAsync(cancellationToken);
        return [.. coupons.Select(coupon => coupon.ToDto())];
    }

    public async Task<CouponDto> SaveCouponAsync(SaveCouponRequest request, bool create, CancellationToken cancellationToken = default)
    {
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length is < 3 or > 40 || !code.All(char.IsAsciiLetterOrDigit))
        {
            throw new ShopException("The coupon code must be 3-40 letters or digits.");
        }

        if (request.Type == CouponType.Percentage && request.Value is <= 0 or > 100)
        {
            throw new ShopException("A percentage coupon needs a value between 0 and 100.");
        }

        if (request.Type == CouponType.FixedAmount && request.Value <= 0)
        {
            throw new ShopException("A fixed-amount coupon needs a value greater than zero.");
        }

        if (request.MinSubtotal < 0 || request.MaxRedemptions is < 1)
        {
            throw new ShopException("Minimum subtotal and maximum redemptions must be positive.");
        }

        var coupon = await db.Coupons.FirstOrDefaultAsync(candidate => candidate.Code == code, cancellationToken);
        if (create)
        {
            if (coupon is not null)
            {
                throw ShopException.Conflict($"Coupon {code} already exists.");
            }

            coupon = new Coupon { Code = code };
            db.Coupons.Add(coupon);
        }
        else if (coupon is null)
        {
            throw ShopException.NotFound($"Coupon {code} was not found.");
        }

        coupon.Description = (request.Description ?? string.Empty).Trim();
        coupon.Type = request.Type;
        coupon.Value = request.Type == CouponType.FreeShipping ? 0 : request.Value;
        coupon.MinSubtotal = request.MinSubtotal;
        coupon.ExpiresAt = request.ExpiresAt;
        coupon.MaxRedemptions = request.MaxRedemptions;
        coupon.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return coupon.ToDto();
    }

    public async Task DeleteCouponAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        var coupon = await db.Coupons.FirstOrDefaultAsync(candidate => candidate.Code == normalized, cancellationToken)
            ?? throw ShopException.NotFound($"Coupon {normalized} was not found.");
        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var orders = db.Orders.AsNoTracking();
        var revenue = await orders.Where(order => order.Status != OrderStatus.Cancelled)
            .SumAsync(order => (double?)order.Total, cancellationToken) ?? 0d;
        var lowStock = await db.Products.AsNoTracking().Include(product => product.Category)
            .Where(product => product.IsActive && product.Stock <= LowStockThreshold)
            .OrderBy(product => product.Stock).ThenBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            await orders.CountAsync(cancellationToken),
            await orders.CountAsync(order => order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing, cancellationToken),
            Pricing.Round((decimal)revenue),
            await db.Products.CountAsync(product => product.IsActive, cancellationToken),
            lowStock.Count(product => product.Stock > 0),
            lowStock.Count(product => product.Stock == 0),
            await db.Users.CountAsync(user => user.Role == UserRole.Shopper, cancellationToken),
            [.. lowStock.Select(product => product.ToAdminDto())]);
    }

    private async Task ApplyAsync(Product product, SaveProductRequest request, CancellationToken cancellationToken)
    {
        var categoryName = (request.Category ?? string.Empty).Trim();
        if (categoryName.Length == 0)
        {
            throw new ShopException("Choose a category.");
        }

        var category = await db.Categories.FirstOrDefaultAsync(candidate => candidate.Name.ToLower() == categoryName.ToLower(), cancellationToken)
            ?? new Category { Name = categoryName };

        product.Name = (request.Name ?? string.Empty).Trim();
        product.Description = (request.Description ?? string.Empty).Trim();
        product.Category = category;
        product.Price = request.Price;
        product.OriginalPrice = request.OriginalPrice;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? "images/product-placeholder.svg"
            : request.ImageUrl.Trim();

        try
        {
            product.Validate();
        }
        catch (ArgumentException exception) when (exception is not ArgumentNullException)
        {
            throw new ShopException("Name and description are required.");
        }

        if (product.OriginalPrice is { } original && original <= product.Price)
        {
            product.OriginalPrice = null;
        }
    }
}
