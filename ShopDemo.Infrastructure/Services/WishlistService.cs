using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

public sealed class WishlistService(ShopDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<ProductSummaryDto>> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rows = await db.WishlistItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.Product.IsActive)
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new
            {
                item.Product.Id,
                item.Product.Name,
                item.Product.Description,
                Category = item.Product.Category.Name,
                item.Product.Price,
                item.Product.OriginalPrice,
                item.Product.ImageUrl,
                item.Product.Stock,
                Rating = item.Product.Reviews.Average(review => (double?)review.Rating) ?? 0d,
                Reviews = item.Product.Reviews.Count()
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new ProductSummaryDto(
            row.Id, row.Name, row.Description, row.Category, row.Price, row.OriginalPrice, row.ImageUrl,
            row.Stock, Math.Round(row.Rating, 1), row.Reviews, true))];
    }

    public async Task AddAsync(int userId, string productId, CancellationToken cancellationToken = default)
    {
        if (!await db.Products.AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken))
        {
            throw ShopException.NotFound($"Product '{productId}' was not found.");
        }

        if (await db.WishlistItems.AnyAsync(item => item.UserId == userId && item.ProductId == productId, cancellationToken))
        {
            return;
        }

        db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId, AddedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(int userId, string productId, CancellationToken cancellationToken = default) =>
        await db.WishlistItems
            .Where(item => item.UserId == userId && item.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
}
