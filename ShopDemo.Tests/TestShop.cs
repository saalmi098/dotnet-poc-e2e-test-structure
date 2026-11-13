using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShopDemo.Infrastructure.Data;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Tests;

/// <summary>A seeded in-memory SQLite database plus the services under test. One instance per test.</summary>
public sealed class TestShop : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestShop()
    {
        _connection.Open();
        Db = CreateContext();
        new DatabaseSeeder(Db, Clock).InitializeAsync().GetAwaiter().GetResult();
        Accounts = new AccountService(Db, Clock);
        Catalog = new CatalogService(Db, Clock);
        Wishlist = new WishlistService(Db, Clock);
        Carts = new CartService(Db, Clock);
        Orders = new OrderService(Db, Carts, Clock);
        Admin = new AdminService(Db, Clock);
    }

    public TimeProvider Clock { get; } = TimeProvider.System;
    public ShopDbContext Db { get; }
    public AccountService Accounts { get; }
    public CatalogService Catalog { get; }
    public WishlistService Wishlist { get; }
    public CartService Carts { get; }
    public OrderService Orders { get; }
    public AdminService Admin { get; }

    public int DemoShopperId => UserId("demo@test.com");
    public int ManagerId => UserId("admin@test.com");
    public int JaneId => UserId("jane@test.com");

    public int UserId(string email) => Db.Users.Single(user => user.Email == email).Id;

    public int StockOf(string productId) => Db.Products.AsNoTracking().Single(product => product.Id == productId).Stock;

    private ShopDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ShopDbContext>().UseSqlite(_connection).Options);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
