using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure.Data;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderStatusEntry> OrderStatusEntries => Set<OrderStatusEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or aggregate decimal / DateTimeOffset columns in SQL, so store them as numbers.
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<string>().HaveMaxLength(2000);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(254);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasMaxLength(64);
            entity.HasOne(product => product.Category).WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(product => product.Reviews).WithOne(review => review.Product)
                .HasForeignKey(review => review.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(product => product.IsAvailable);
            entity.Ignore(product => product.IsOnSale);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(review => new { review.ProductId, review.UserId }).IsUnique();
            entity.HasOne(review => review.User).WithMany().HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(item => new { item.UserId, item.ProductId });
            entity.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(cart => cart.UserId);
            entity.HasOne<User>().WithMany().HasForeignKey(cart => cart.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(cart => cart.Items).WithOne().HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(item => new { item.CartId, item.ProductId });
            entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(coupon => coupon.Code);
            entity.Property(coupon => coupon.Code).HasMaxLength(40);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(order => order.Number).IsUnique();
            entity.HasIndex(order => order.UserId);
            entity.HasOne(order => order.User).WithMany().HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(order => order.Lines).WithOne().HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(order => order.History).WithOne().HasForeignKey(entry => entry.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(order => order.ShippingMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(order => order.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Ignore(line => line.LineTotal);
        });

        modelBuilder.Entity<OrderStatusEntry>(entity =>
        {
            entity.Property(status => status.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<User>().Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Coupon>().Property(coupon => coupon.Type).HasConversion<string>().HasMaxLength(20);
    }
}
