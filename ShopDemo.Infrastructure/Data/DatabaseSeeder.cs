using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure.Data;

/// <summary>Creates the schema and fills a fresh database with the demo catalog, accounts, coupons, reviews and orders.</summary>
public sealed class DatabaseSeeder(ShopDbContext db, TimeProvider clock)
{
    public const string DemoPassword = "1234";

    // Hashing is deliberately slow, so hash the shared demo password once; the salt is embedded in the hash,
    // which makes reusing it for several demo accounts harmless. Resetting the database stays fast.
    private static readonly Lazy<string> DemoPasswordHash = new(() => new PasswordHasher<User>().HashPassword(new User(), DemoPassword));

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    /// <summary>Deletes all data and seeds it again, keeping the database file (and any open connections) intact.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.OrderStatusEntries.ExecuteDeleteAsync(cancellationToken);
        await db.OrderLines.ExecuteDeleteAsync(cancellationToken);
        await db.Orders.ExecuteDeleteAsync(cancellationToken);
        await db.Reviews.ExecuteDeleteAsync(cancellationToken);
        await db.WishlistItems.ExecuteDeleteAsync(cancellationToken);
        await db.CartItems.ExecuteDeleteAsync(cancellationToken);
        await db.Carts.ExecuteDeleteAsync(cancellationToken);
        await db.Coupons.ExecuteDeleteAsync(cancellationToken);
        await db.Products.ExecuteDeleteAsync(cancellationToken);
        await db.Categories.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);
        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence", cancellationToken);
        }
        else if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER SEQUENCE \"Users_Id_seq\" RESTART; ALTER SEQUENCE \"Categories_Id_seq\" RESTART; " +
                "ALTER SEQUENCE \"Orders_Id_seq\" RESTART; ALTER SEQUENCE \"OrderLines_Id_seq\" RESTART; " +
                "ALTER SEQUENCE \"OrderStatusEntries_Id_seq\" RESTART; ALTER SEQUENCE \"Reviews_Id_seq\" RESTART",
                cancellationToken);
        }
        db.ChangeTracker.Clear();
        await SeedAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        User NewUser(string email, string name, UserRole role, string? address = null, string? postal = null, string? city = null)
        {
            var user = new User
            {
                Email = email,
                DisplayName = name,
                Role = role,
                CreatedAt = now.AddDays(-60),
                AddressLine = address,
                PostalCode = postal,
                City = city,
                Country = address is null ? null : "Austria"
            };
            user.PasswordHash = DemoPasswordHash.Value;
            return user;
        }

        var demo = NewUser("demo@test.com", "Demo Shopper", UserRole.Shopper, "Demo Street 1", "1010", "Vienna");
        var admin = NewUser("admin@test.com", "Demo Store Manager", UserRole.StoreManager);
        var jane = NewUser("jane@test.com", "Jane Doe", UserRole.Shopper, "Example Road 7", "8010", "Graz");
        db.Users.AddRange(demo, admin, jane);

        var categories = new[] { "Stationery", "Home", "Accessories", "Kitchen", "Tech", "Outdoor" }
            .ToDictionary(name => name, name => new Category { Name = name });
        db.Categories.AddRange(categories.Values);

        var sortOrder = 0;
        Product P(string id, string name, string category, decimal price, int stock, string description,
            decimal? originalPrice = null, int daysAgo = 90) => new()
        {
            Id = id,
            Name = name,
            Category = categories[category],
            Price = price,
            OriginalPrice = originalPrice,
            Stock = stock,
            Description = description,
            ImageUrl = $"images/{category.ToLowerInvariant()}.svg",
            SortOrder = ++sortOrder,
            CreatedAt = now.AddDays(-daysAgo)
        };

        var products = new[]
        {
            P("field-notebook", "Field Notes Notebook", "Stationery", 12.50m, 18,
                "A durable, pocket-sized notebook with dotted pages for everyday ideas and sketches."),
            P("ceramic-mug", "Everyday Ceramic Mug", "Home", 24.00m, 12,
                "A comfortable stoneware mug with a smooth glaze and a generous 350 ml capacity."),
            P("canvas-tote", "Market Canvas Tote", "Accessories", 19.95m, 0,
                "A sturdy reusable cotton tote with reinforced handles for daily errands."),
            P("desk-lamp", "Adjustable Desk Lamp", "Home", 46.00m, 6,
                "A compact LED lamp with a warm light setting and an adjustable neck."),
            P("steel-bottle", "Insulated Steel Bottle", "Accessories", 32.75m, 9,
                "A leak-resistant stainless-steel bottle that keeps drinks cold during a long day out."),
            P("pencil-set", "Graphite Pencil Set", "Stationery", 8.00m, 25,
                "A set of six sharpened graphite pencils in a range of grades for writing and drawing."),
            P("fountain-pen", "Brass Fountain Pen", "Stationery", 39.90m, 7,
                "A weighty brass pen with a smooth medium nib and a screw-on cap.", daysAgo: 80),
            P("wool-blanket", "Merino Wool Blanket", "Home", 89.00m, 4,
                "A soft, breathable merino throw for sofas and reading corners.", 119.00m, 70),
            P("pour-over-set", "Pour-Over Coffee Set", "Kitchen", 54.00m, 10,
                "A glass dripper and carafe with reusable steel filter for a clean morning cup.", daysAgo: 60),
            P("chef-knife", "Carbon Steel Chef Knife", "Kitchen", 68.50m, 5,
                "A razor-sharp 20 cm chef knife with an oak handle.", daysAgo: 55),
            P("cutting-board", "Walnut Cutting Board", "Kitchen", 35.00m, 14,
                "A thick end-grain board that is gentle on knives and easy to refinish.", 42.00m, 50),
            P("wireless-earbuds", "Wireless Earbuds", "Tech", 79.00m, 20,
                "Bluetooth earbuds with eight hours of playback and a pocket-sized charging case.", daysAgo: 40),
            P("usb-charger", "65 W USB-C Charger", "Tech", 29.90m, 30,
                "A compact GaN charger that powers a laptop and a phone at the same time.", daysAgo: 35),
            P("mechanical-keyboard", "Compact Mechanical Keyboard", "Tech", 119.00m, 3,
                "A 75% hot-swappable keyboard with tactile switches and a USB-C cable.", 139.00m, 30),
            P("hiking-backpack", "Trail Hiking Backpack", "Outdoor", 98.00m, 8,
                "A 28 litre daypack with a ventilated back panel and rain cover.", daysAgo: 25),
            P("camp-stove", "Folding Camp Stove", "Outdoor", 42.00m, 0,
                "An ultralight folding stove that packs down to the size of a deck of cards.", daysAgo: 20),
        };
        db.Products.AddRange(products);

        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", Description = "10% off your order", Type = CouponType.Percentage, Value = 10 },
            new Coupon { Code = "SAVE5", Description = "5.00 off orders from 30.00", Type = CouponType.FixedAmount, Value = 5, MinSubtotal = 30 },
            new Coupon { Code = "FREESHIP", Description = "Free shipping", Type = CouponType.FreeShipping },
            new Coupon { Code = "OLD20", Description = "Expired 20% coupon", Type = CouponType.Percentage, Value = 20, ExpiresAt = now.AddDays(-10) },
            new Coupon { Code = "ONCE", Description = "Single-use coupon, already redeemed", Type = CouponType.Percentage, Value = 15, MaxRedemptions = 1, Redemptions = 1 });

        await db.SaveChangesAsync(cancellationToken);

        db.Reviews.AddRange(
            Review("field-notebook", jane, 5, "Perfect size", "Fits in every pocket and the paper takes ink well.", now.AddDays(-20)),
            Review("field-notebook", demo, 4, "Solid notebook", "Great quality, I wish it had more pages.", now.AddDays(-12)),
            Review("ceramic-mug", jane, 5, "My daily mug", "Keeps coffee warm and feels great in the hand.", now.AddDays(-18)),
            Review("desk-lamp", demo, 3, "Good light, wobbly neck", "The light is lovely but the joint is a bit loose.", now.AddDays(-9)),
            Review("steel-bottle", jane, 4, "Keeps things cold", "Ice was still there after a whole day.", now.AddDays(-7)),
            Review("wireless-earbuds", demo, 2, "Disappointing battery", "Lasts about five hours, not eight.", now.AddDays(-5)),
            Review("wireless-earbuds", jane, 4, "Good for the price", "Clear sound and a comfortable fit.", now.AddDays(-4)));

        db.WishlistItems.Add(new WishlistItem { UserId = demo.Id, ProductId = "desk-lamp", AddedAt = now.AddDays(-3) });

        db.Orders.AddRange(
            SeedOrder("ORD-SEED0001", jane, OrderStatus.Delivered, now.AddDays(-30), products, ("field-notebook", 2), ("pencil-set", 1)),
            SeedOrder("ORD-SEED0002", jane, OrderStatus.Shipped, now.AddDays(-3), products, ("steel-bottle", 1)),
            SeedOrder("ORD-SEED0003", demo, OrderStatus.Delivered, now.AddDays(-21), products, ("ceramic-mug", 2)));

        await db.SaveChangesAsync(cancellationToken);

        Review Review(string productId, User user, int rating, string title, string comment, DateTimeOffset createdAt) => new()
        {
            ProductId = productId,
            User = user,
            Rating = rating,
            Title = title,
            Comment = comment,
            CreatedAt = createdAt
        };
    }

    private static Order SeedOrder(
        string number,
        User user,
        OrderStatus status,
        DateTimeOffset createdAt,
        IEnumerable<Product> products,
        params (string ProductId, int Quantity)[] lines)
    {
        var catalog = products.ToDictionary(product => product.Id);
        var orderLines = lines.Select(line => new OrderLine
        {
            ProductId = line.ProductId,
            ProductName = catalog[line.ProductId].Name,
            UnitPrice = catalog[line.ProductId].Price,
            Quantity = line.Quantity
        }).ToList();
        var breakdown = Pricing.Calculate(orderLines.Sum(line => line.LineTotal), null, ShippingMethod.Standard);

        var history = new List<OrderStatusEntry> { new() { Status = OrderStatus.Pending, At = createdAt, Note = "Order placed" } };
        var progress = new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
        var step = 0;
        foreach (var next in progress)
        {
            if (next > status)
            {
                break;
            }

            history.Add(new OrderStatusEntry { Status = next, At = createdAt.AddDays(++step) });
        }

        return new Order
        {
            Number = number,
            User = user,
            Status = status,
            CreatedAt = createdAt,
            CustomerName = user.DisplayName,
            CustomerEmail = user.Email,
            AddressLine = user.AddressLine ?? "Demo Street 1",
            PostalCode = user.PostalCode ?? "1010",
            City = user.City ?? "Vienna",
            Country = user.Country ?? "Austria",
            ShippingMethod = ShippingMethod.Standard,
            PaymentMethod = PaymentMethod.Invoice,
            PaymentReference = "Invoice",
            Subtotal = breakdown.Subtotal,
            Discount = breakdown.Discount,
            ShippingCost = breakdown.ShippingCost,
            Total = breakdown.Total,
            Lines = orderLines,
            History = history
        };
    }
}
