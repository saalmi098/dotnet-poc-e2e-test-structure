using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

public sealed class CatalogService(ShopDbContext db, TimeProvider clock)
{
    public const int MaxPageSize = 48;

    public async Task<PagedResult<ProductSummaryDto>> SearchAsync(
        ProductQuery query,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(1, query.Page);

        var products = db.Products.AsNoTracking().Where(product => product.IsActive);

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLower();
            products = products.Where(product =>
                product.Name.ToLower().Contains(term) || product.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim().ToLower();
            products = products.Where(product => product.Category.Name.ToLower() == category);
        }

        if (query.MinPrice is { } min)
        {
            products = products.Where(product => product.Price >= min);
        }

        if (query.MaxPrice is { } max)
        {
            products = products.Where(product => product.Price <= max);
        }

        if (query.InStockOnly)
        {
            products = products.Where(product => product.Stock > 0);
        }

        if (query.OnSaleOnly)
        {
            products = products.Where(product => product.OriginalPrice > product.Price);
        }

        var total = await products.CountAsync(cancellationToken);

        var projected = products.Select(product => new
        {
            Product = product,
            Rating = product.Reviews.Average(review => (double?)review.Rating) ?? 0d,
            Reviews = product.Reviews.Count()
        });

        projected = query.Sort switch
        {
            ProductSort.PriceAscending => projected.OrderBy(row => row.Product.Price).ThenBy(row => row.Product.SortOrder),
            ProductSort.PriceDescending => projected.OrderByDescending(row => row.Product.Price).ThenBy(row => row.Product.SortOrder),
            ProductSort.NameAscending => projected.OrderBy(row => row.Product.Name),
            ProductSort.TopRated => projected.OrderByDescending(row => row.Rating).ThenByDescending(row => row.Reviews).ThenBy(row => row.Product.SortOrder),
            ProductSort.Newest => projected.OrderByDescending(row => row.Product.CreatedAt).ThenBy(row => row.Product.SortOrder),
            _ => projected.OrderBy(row => row.Product.SortOrder).ThenBy(row => row.Product.Name)
        };

        var rows = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new
            {
                row.Product.Id,
                row.Product.Name,
                row.Product.Description,
                Category = row.Product.Category.Name,
                row.Product.Price,
                row.Product.OriginalPrice,
                row.Product.ImageUrl,
                row.Product.Stock,
                row.Rating,
                row.Reviews
            })
            .ToListAsync(cancellationToken);

        var wishlisted = userId is { } id
            ? (await db.WishlistItems.Where(item => item.UserId == id).Select(item => item.ProductId)
                .ToListAsync(cancellationToken)).ToHashSet()
            : [];

        var items = rows.Select(row => new ProductSummaryDto(
            row.Id, row.Name, row.Description, row.Category, row.Price, row.OriginalPrice, row.ImageUrl,
            row.Stock, Math.Round(row.Rating, 1), row.Reviews, wishlisted.Contains(row.Id))).ToList();

        return new PagedResult<ProductSummaryDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await db.Categories.AsNoTracking()
            .Where(category => category.Products.Any(product => product.IsActive))
            .OrderBy(category => category.Name)
            .Select(category => new { category.Id, category.Name, Count = category.Products.Count(product => product.IsActive) })
            .ToListAsync(cancellationToken))
        .Select(row => new CategoryDto(row.Id, row.Name, row.Count))
        .ToList();

    public async Task<ProductDetailDto> GetProductAsync(string productId, int? userId, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.AsNoTracking().Include(candidate => candidate.Category)
            .FirstOrDefaultAsync(candidate => candidate.Id == productId && candidate.IsActive, cancellationToken)
            ?? throw ShopException.NotFound($"Product '{productId}' was not found.");

        var ratings = await db.Reviews.Where(review => review.ProductId == productId)
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var distribution = Enumerable.Range(1, 5).Select(star => ratings.FirstOrDefault(r => r.Rating == star)?.Count ?? 0).ToList();
        var count = distribution.Sum();
        var average = count == 0 ? 0 : Math.Round(distribution.Select((n, i) => n * (i + 1)).Sum() / (double)count, 1);
        var inWishlist = userId is { } id &&
            await db.WishlistItems.AnyAsync(item => item.UserId == id && item.ProductId == productId, cancellationToken);

        return new ProductDetailDto(
            product.Id, product.Name, product.Description, product.Category.Name, product.Price,
            product.OriginalPrice, product.ImageUrl, product.Stock, average, count, inWishlist, distribution);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetReviewsAsync(string productId, CancellationToken cancellationToken = default) =>
        await db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == productId)
            .OrderByDescending(review => review.CreatedAt)
            .Take(100)
            .Select(review => new ReviewDto(
                review.Id, review.ProductId, review.UserId, review.User.DisplayName,
                review.Rating, review.Title, review.Comment, review.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<ReviewDto> AddReviewAsync(
        int userId,
        string productId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ShopException.Unauthorized("Log in to write a review.");
        if (user.Role != UserRole.Shopper)
        {
            throw ShopException.Forbidden("Store managers cannot write product reviews.");
        }

        if (!await db.Products.AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken))
        {
            throw ShopException.NotFound($"Product '{productId}' was not found.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new ShopException("Choose a rating between 1 and 5 stars.");
        }

        var title = (request.Title ?? string.Empty).Trim();
        var comment = (request.Comment ?? string.Empty).Trim();
        if (title.Length == 0 || title.Length > 80)
        {
            throw new ShopException("Enter a review title of up to 80 characters.");
        }

        if (comment.Length == 0 || comment.Length > 1000)
        {
            throw new ShopException("Enter a review text of up to 1000 characters.");
        }

        if (await db.Reviews.AnyAsync(review => review.ProductId == productId && review.UserId == userId, cancellationToken))
        {
            throw ShopException.Conflict("You have already reviewed this product.");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = request.Rating,
            Title = title,
            Comment = comment,
            CreatedAt = clock.GetUtcNow()
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        return new ReviewDto(review.Id, productId, userId, user.DisplayName, review.Rating, title, comment, review.CreatedAt);
    }

    public async Task DeleteReviewAsync(int reviewId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ShopException.Unauthorized("Log in to delete a review.");
        var review = await db.Reviews.FirstOrDefaultAsync(candidate => candidate.Id == reviewId, cancellationToken)
            ?? throw ShopException.NotFound("Review not found.");

        if (review.UserId != userId && user.Role != UserRole.StoreManager)
        {
            throw ShopException.Forbidden("You can only delete your own reviews.");
        }

        db.Reviews.Remove(review);
        await db.SaveChangesAsync(cancellationToken);
    }
}
