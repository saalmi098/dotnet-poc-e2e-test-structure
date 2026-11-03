using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

/// <summary>Identifies whose cart is meant: a signed-in account, or an anonymous visitor identified by a cookie value.</summary>
public readonly record struct CartOwner(int? UserId, Guid? GuestId)
{
    public static CartOwner ForUser(int userId) => new(userId, null);
    public static CartOwner ForGuest(Guid guestId) => new(null, guestId);
}

public sealed class CartService(ShopDbContext db, TimeProvider clock)
{
    public async Task<CartDto> GetAsync(CartOwner owner, ShippingMethod shippingMethod = ShippingMethod.Standard, CancellationToken cancellationToken = default)
    {
        var cart = await FindAsync(owner, cancellationToken);
        return await BuildAsync(cart, shippingMethod, cancellationToken);
    }

    public async Task<CartDto> AddAsync(CartOwner owner, string productId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ShopException("Quantity must be greater than zero.");
        }

        var product = await FindProductAsync(productId, cancellationToken);
        var cart = await GetOrCreateAsync(owner, cancellationToken);
        var existing = cart.Items.FirstOrDefault(item => item.ProductId == productId);
        var newQuantity = checked((existing?.Quantity ?? 0) + quantity);
        EnsureQuantityAllowed(product, newQuantity);

        if (existing is null)
        {
            cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = newQuantity });
        }
        else
        {
            existing.Quantity = newQuantity;
        }

        return await SaveAsync(cart, cancellationToken);
    }

    public async Task<CartDto> SetQuantityAsync(CartOwner owner, string productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            throw new ShopException("Quantity cannot be negative.");
        }

        var cart = await GetOrCreateAsync(owner, cancellationToken);
        var existing = cart.Items.FirstOrDefault(item => item.ProductId == productId)
            ?? throw ShopException.NotFound("That product is not in your cart.");

        if (quantity == 0)
        {
            cart.Items.Remove(existing);
            db.CartItems.Remove(existing);
        }
        else
        {
            var product = await FindProductAsync(productId, cancellationToken);
            EnsureQuantityAllowed(product, quantity);
            existing.Quantity = quantity;
        }

        return await SaveAsync(cart, cancellationToken);
    }

    public Task<CartDto> RemoveAsync(CartOwner owner, string productId, CancellationToken cancellationToken = default) =>
        SetQuantityAsync(owner, productId, 0, cancellationToken);

    public async Task<CartDto> ClearAsync(CartOwner owner, CancellationToken cancellationToken = default)
    {
        var cart = await FindAsync(owner, cancellationToken);
        if (cart is not null)
        {
            db.Carts.Remove(cart);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await BuildAsync(null, ShippingMethod.Standard, cancellationToken);
    }

    public async Task<CartDto> ApplyCouponAsync(CartOwner owner, string? code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            throw new ShopException("Enter a coupon code.");
        }

        var cart = await FindAsync(owner, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new ShopException("Add something to your cart before applying a coupon.");
        }

        var coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Code == normalized, cancellationToken)
            ?? throw new ShopException($"Coupon {normalized} was not found.");

        var subtotal = await SubtotalAsync(cart, cancellationToken);
        Pricing.EnsureCouponUsable(coupon, subtotal, clock.GetUtcNow());

        cart.CouponCode = coupon.Code;
        return await SaveAsync(cart, cancellationToken);
    }

    public async Task<CartDto> RemoveCouponAsync(CartOwner owner, CancellationToken cancellationToken = default)
    {
        var cart = await FindAsync(owner, cancellationToken);
        if (cart is not null)
        {
            cart.CouponCode = null;
            cart.UpdatedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }

        return await BuildAsync(cart, ShippingMethod.Standard, cancellationToken);
    }

    /// <summary>Moves the anonymous visitor's items into the account's cart after login.</summary>
    public async Task MergeGuestCartAsync(Guid guestId, int userId, CancellationToken cancellationToken = default)
    {
        var guestCart = await FindAsync(CartOwner.ForGuest(guestId), cancellationToken);
        if (guestCart is null)
        {
            return;
        }

        var userCart = await GetOrCreateAsync(CartOwner.ForUser(userId), cancellationToken);
        foreach (var guestItem in guestCart.Items)
        {
            var product = await db.Products.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == guestItem.ProductId && candidate.IsActive, cancellationToken);
            if (product is null || !product.IsAvailable)
            {
                continue;
            }

            var existing = userCart.Items.FirstOrDefault(item => item.ProductId == guestItem.ProductId);
            var limit = Math.Min(product.Stock, Pricing.MaxQuantityPerLine);
            var merged = Math.Min((existing?.Quantity ?? 0) + guestItem.Quantity, limit);
            if (existing is null)
            {
                userCart.Items.Add(new CartItem { CartId = userCart.Id, ProductId = guestItem.ProductId, Quantity = merged });
            }
            else
            {
                existing.Quantity = merged;
            }
        }

        userCart.CouponCode ??= guestCart.CouponCode;
        userCart.UpdatedAt = clock.GetUtcNow();
        db.Carts.Remove(guestCart);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart?> FindAsync(CartOwner owner, CancellationToken cancellationToken)
    {
        var query = db.Carts.Include(cart => cart.Items).ThenInclude(item => item.Product).AsQueryable();
        if (owner.UserId is { } userId)
        {
            return await query.FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
        }

        if (owner.GuestId is { } guestId)
        {
            return await query.FirstOrDefaultAsync(cart => cart.Id == guestId && cart.UserId == null, cancellationToken);
        }

        return null;
    }

    private async Task<Cart> GetOrCreateAsync(CartOwner owner, CancellationToken cancellationToken)
    {
        var cart = await FindAsync(owner, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            Id = owner.GuestId ?? Guid.NewGuid(),
            UserId = owner.UserId,
            UpdatedAt = clock.GetUtcNow()
        };
        db.Carts.Add(cart);
        return cart;
    }

    private async Task<Product> FindProductAsync(string productId, CancellationToken cancellationToken) =>
        await db.Products.FirstOrDefaultAsync(product => product.Id == productId && product.IsActive, cancellationToken)
        ?? throw ShopException.NotFound($"Product '{productId}' was not found.");

    private static void EnsureQuantityAllowed(Product product, int quantity)
    {
        if (!product.IsAvailable)
        {
            throw new ShopException($"{product.Name} is out of stock.");
        }

        if (quantity > product.Stock)
        {
            throw new ShopException($"Only {product.Stock} unit(s) of {product.Name} are available.");
        }

        if (quantity > Pricing.MaxQuantityPerLine)
        {
            throw new ShopException($"You can order at most {Pricing.MaxQuantityPerLine} units of {product.Name}.");
        }
    }

    private async Task<CartDto> SaveAsync(Cart cart, CancellationToken cancellationToken)
    {
        cart.UpdatedAt = clock.GetUtcNow();
        if (cart.Items.Count == 0)
        {
            cart.CouponCode = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await BuildAsync(cart, ShippingMethod.Standard, cancellationToken);
    }

    private async Task<decimal> SubtotalAsync(Cart cart, CancellationToken cancellationToken)
    {
        await db.Entry(cart).Collection(c => c.Items).Query().Include(i => i.Product).LoadAsync(cancellationToken);
        return Pricing.Round(cart.Items.Sum(item => item.Product.Price * item.Quantity));
    }

    internal async Task<CartDto> BuildAsync(Cart? cart, ShippingMethod shippingMethod, CancellationToken cancellationToken)
    {
        if (cart is null || cart.Items.Count == 0)
        {
            return new CartDto([], 0, null, 0, 0, 0, 0, 0, shippingMethod, Pricing.FreeStandardShippingThreshold);
        }

        var lines = cart.Items
            .Where(item => item.Product.IsActive)
            .OrderBy(item => item.Product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new CartLineDto(
                item.ProductId, item.Product.Name, item.Product.ImageUrl, item.Product.Price, item.Quantity, item.Product.Stock))
            .ToList();
        var subtotal = Pricing.Round(lines.Sum(line => line.LineTotal));

        Coupon? coupon = null;
        if (cart.CouponCode is { } code)
        {
            coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Code == code, cancellationToken);
            try
            {
                if (coupon is null)
                {
                    throw new ShopException("missing");
                }

                Pricing.EnsureCouponUsable(coupon, subtotal, clock.GetUtcNow());
            }
            catch (ShopException)
            {
                // The coupon stopped being valid (expired, minimum no longer met, ...): drop it quietly.
                coupon = null;
                cart.CouponCode = null;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        var breakdown = Pricing.Calculate(subtotal, coupon, shippingMethod);
        var remaining = shippingMethod == ShippingMethod.Standard && coupon?.Type != CouponType.FreeShipping
            ? Math.Max(0, Pricing.FreeStandardShippingThreshold - (subtotal - breakdown.Discount))
            : 0;

        return new CartDto(
            lines,
            lines.Sum(line => line.Quantity),
            coupon?.Code,
            breakdown.Subtotal,
            breakdown.Discount,
            breakdown.ShippingCost,
            breakdown.Total,
            breakdown.IncludedVat,
            shippingMethod,
            Pricing.Round(remaining));
    }

    internal Task<Cart?> FindForCheckoutAsync(int userId, CancellationToken cancellationToken) =>
        FindAsync(CartOwner.ForUser(userId), cancellationToken);
}
