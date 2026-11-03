using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopDemo.Infrastructure.Data;
using ShopDemo.Infrastructure.Services;

namespace ShopDemo.Infrastructure;

public enum DatabaseProvider
{
    Sqlite,
    Postgres
}

public static class InfrastructureRegistration
{
    public static IServiceCollection AddShopInfrastructure(
        this IServiceCollection services,
        string connectionString,
        DatabaseProvider provider = DatabaseProvider.Sqlite)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ShopDbContext>(options =>
        {
            if (provider == DatabaseProvider.Postgres)
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<AccountService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<WishlistService>();
        services.AddScoped<CartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<AdminService>();
        return services;
    }
}
