using ShopDemo.Contracts;
using ShopDemo.Contracts.Catalog;
using ShopDemo.Domain;
using Xunit;

namespace ShopDemo.Tests;

public sealed class CatalogServiceTests : IDisposable
{
    private readonly TestShop _shop = new();

    public void Dispose() => _shop.Dispose();

    private Task<PagedResult<ProductSummaryDto>> Search(ProductQuery query) => _shop.Catalog.SearchAsync(query, null);

    [Fact]
    public async Task Default_listing_starts_with_the_original_demo_products()
    {
        var result = await Search(new ProductQuery());

        Assert.Equal(16, result.TotalCount);
        Assert.Equal(12, result.Items.Count);
        Assert.Equal(
            ["field-notebook", "ceramic-mug", "canvas-tote", "desk-lamp", "steel-bottle", "pencil-set"],
            result.Items.Take(6).Select(product => product.Id));
    }

    [Fact]
    public async Task Search_for_tote_finds_exactly_the_tote()
    {
        var result = await Search(new ProductQuery(Search: "Tote"));

        Assert.Equal("canvas-tote", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Search_without_matches_returns_nothing()
    {
        var result = await Search(new ProductQuery(Search: "no product has this name"));

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Category_filter_is_case_insensitive_and_narrows_the_result()
    {
        var result = await Search(new ProductQuery(Category: "home"));

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, product => Assert.Equal("Home", product.Category));
    }

    [Fact]
    public async Task Price_range_and_in_stock_filters_combine()
    {
        var result = await Search(new ProductQuery(MinPrice: 20, MaxPrice: 50, InStockOnly: true));

        Assert.All(result.Items, product =>
        {
            Assert.InRange(product.Price, 20m, 50m);
            Assert.True(product.IsAvailable);
        });
        Assert.DoesNotContain(result.Items, product => product.Id == "canvas-tote");
    }

    [Fact]
    public async Task On_sale_filter_returns_only_discounted_products()
    {
        var result = await Search(new ProductQuery(OnSaleOnly: true));

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, product => Assert.True(product.IsOnSale));
    }

    [Fact]
    public async Task Sorting_by_price_orders_the_results()
    {
        var ascending = await Search(new ProductQuery(Sort: ProductSort.PriceAscending, PageSize: 48));
        var descending = await Search(new ProductQuery(Sort: ProductSort.PriceDescending, PageSize: 48));

        Assert.Equal(ascending.Items.Select(product => product.Price).Order(), ascending.Items.Select(product => product.Price));
        Assert.Equal(descending.Items.Select(product => product.Price).OrderDescending(), descending.Items.Select(product => product.Price));
    }

    [Fact]
    public async Task Sorting_by_rating_puts_the_best_rated_first()
    {
        var result = await Search(new ProductQuery(Sort: ProductSort.TopRated));

        Assert.Equal("ceramic-mug", result.Items[0].Id);
    }

    [Fact]
    public async Task Pagination_splits_the_catalog_into_pages()
    {
        var first = await Search(new ProductQuery(Page: 1, PageSize: 10));
        var second = await Search(new ProductQuery(Page: 2, PageSize: 10));

        Assert.Equal(10, first.Items.Count);
        Assert.Equal(6, second.Items.Count);
        Assert.Equal(2, first.TotalPages);
        Assert.Empty(first.Items.Select(product => product.Id).Intersect(second.Items.Select(product => product.Id)));
    }

    [Fact]
    public async Task Inactive_products_are_hidden_from_the_shop()
    {
        await _shop.Admin.UpdateProductAsync("ceramic-mug",
            new SaveProductRequest("ceramic-mug", "Everyday Ceramic Mug", "A mug.", "Home", 24, null, null, 12, IsActive: false));

        var result = await Search(new ProductQuery(PageSize: 48));

        Assert.DoesNotContain(result.Items, product => product.Id == "ceramic-mug");
        await Assert.ThrowsAsync<ShopException>(() => _shop.Catalog.GetProductAsync("ceramic-mug", null));
    }

    [Fact]
    public async Task Product_details_include_the_rating_distribution()
    {
        var product = await _shop.Catalog.GetProductAsync("wireless-earbuds", null);

        Assert.Equal(2, product.ReviewCount);
        Assert.Equal(3.0, product.AverageRating);
        Assert.Equal([0, 1, 0, 1, 0], product.RatingDistribution);
    }

    [Fact]
    public async Task Categories_list_contains_counts()
    {
        var categories = await _shop.Catalog.GetCategoriesAsync();

        Assert.Equal(6, categories.Count);
        Assert.Equal(3, categories.Single(category => category.Name == "Stationery").ProductCount);
    }

    [Fact]
    public async Task Shopper_can_review_a_product_once()
    {
        var review = await _shop.Catalog.AddReviewAsync(_shop.JaneId, "desk-lamp", new CreateReviewRequest(4, "Nice", "Works well."));

        Assert.Equal("Jane Doe", review.AuthorName);
        var duplicate = await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Catalog.AddReviewAsync(_shop.JaneId, "desk-lamp", new CreateReviewRequest(5, "Again", "Again.")));
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Theory]
    [InlineData(0, "T", "C")]
    [InlineData(6, "T", "C")]
    [InlineData(3, "", "C")]
    [InlineData(3, "T", "")]
    public async Task Invalid_reviews_are_rejected(int rating, string title, string comment) =>
        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Catalog.AddReviewAsync(_shop.JaneId, "desk-lamp", new CreateReviewRequest(rating, title, comment)));

    [Fact]
    public async Task Store_managers_cannot_review_products()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Catalog.AddReviewAsync(_shop.ManagerId, "desk-lamp", new CreateReviewRequest(5, "T", "C")));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task Only_the_author_or_a_manager_can_delete_a_review()
    {
        var review = (await _shop.Catalog.GetReviewsAsync("desk-lamp")).Single();

        var forbidden = await Assert.ThrowsAsync<ShopException>(() => _shop.Catalog.DeleteReviewAsync(review.Id, _shop.JaneId));
        await _shop.Catalog.DeleteReviewAsync(review.Id, _shop.ManagerId);

        Assert.Equal(403, forbidden.StatusCode);
        Assert.Empty(await _shop.Catalog.GetReviewsAsync("desk-lamp"));
    }

    [Fact]
    public async Task Wishlist_add_is_idempotent_and_remove_works()
    {
        var demo = _shop.DemoShopperId;

        await _shop.Wishlist.AddAsync(demo, "ceramic-mug");
        await _shop.Wishlist.AddAsync(demo, "ceramic-mug");
        var withMug = await _shop.Wishlist.GetAsync(demo);
        await _shop.Wishlist.RemoveAsync(demo, "ceramic-mug");
        var without = await _shop.Wishlist.GetAsync(demo);

        Assert.Equal(["ceramic-mug", "desk-lamp"], withMug.Select(product => product.Id).Order());
        Assert.Equal(["desk-lamp"], without.Select(product => product.Id));
        Assert.True((await _shop.Catalog.SearchAsync(new ProductQuery(Search: "lamp"), demo)).Items.Single().IsInWishlist);
    }
}

public sealed class AccountServiceTests : IDisposable
{
    private readonly TestShop _shop = new();

    public void Dispose() => _shop.Dispose();

    [Fact]
    public async Task Demo_accounts_can_log_in_with_any_email_casing()
    {
        var user = await _shop.Accounts.AuthenticateAsync("  Demo@Test.com ", "1234");

        Assert.Equal(UserRole.Shopper, user!.Role);
    }

    [Theory]
    [InlineData("demo@test.com", "wrong")]
    [InlineData("nobody@test.com", "1234")]
    [InlineData("demo@test.com", "")]
    public async Task Wrong_credentials_are_rejected(string email, string password) =>
        Assert.Null(await _shop.Accounts.AuthenticateAsync(email, password));

    [Fact]
    public async Task Registering_creates_a_shopper_that_can_log_in()
    {
        var user = await _shop.Accounts.RegisterAsync(new RegisterRequest("New Person", "New@Example.com", "password1"));

        Assert.Equal(UserRole.Shopper, user.Role);
        Assert.Equal("new@example.com", user.Email);
        Assert.NotNull(await _shop.Accounts.AuthenticateAsync("new@example.com", "password1"));
    }

    [Fact]
    public async Task Registering_with_an_existing_email_conflicts()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Accounts.RegisterAsync(new RegisterRequest("Dup", "demo@test.com", "password1")));

        Assert.Equal(409, exception.StatusCode);
    }

    [Theory]
    [InlineData("", "a@b.com", "password1")]
    [InlineData("Name", "not-an-email", "password1")]
    [InlineData("Name", "a@b.com", "short")]
    public async Task Invalid_registrations_are_rejected(string name, string email, string password) =>
        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Accounts.RegisterAsync(new RegisterRequest(name, email, password)));

    [Fact]
    public async Task Password_change_requires_the_current_password()
    {
        var id = _shop.DemoShopperId;

        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Accounts.ChangePasswordAsync(id, new ChangePasswordRequest("wrong", "newpassword1")));
        await _shop.Accounts.ChangePasswordAsync(id, new ChangePasswordRequest("1234", "newpassword1"));

        Assert.Null(await _shop.Accounts.AuthenticateAsync("demo@test.com", "1234"));
        Assert.NotNull(await _shop.Accounts.AuthenticateAsync("demo@test.com", "newpassword1"));
    }

    [Fact]
    public async Task Profile_update_stores_the_address()
    {
        var user = await _shop.Accounts.UpdateProfileAsync(_shop.DemoShopperId,
            new UpdateProfileRequest("Demo Renamed", "123", "Main 1", "4020", "Linz", "Austria"));

        Assert.Equal("Demo Renamed", user.DisplayName);
        Assert.Equal("Linz", user.City);
    }
}

public sealed class AdminServiceTests : IDisposable
{
    private readonly TestShop _shop = new();

    public void Dispose() => _shop.Dispose();

    private static SaveProductRequest NewProduct(string id = "new-item") =>
        new(id, "New item", "A new item.", "Tech", 10m, 15m, null, 3);

    [Fact]
    public async Task Creating_a_product_makes_it_visible_in_the_catalog()
    {
        await _shop.Admin.CreateProductAsync(NewProduct());

        var product = await _shop.Catalog.GetProductAsync("new-item", null);

        Assert.True(product.IsOnSale);
        Assert.Equal("Tech", product.Category);
    }

    [Fact]
    public async Task Creating_a_product_in_a_new_category_creates_the_category()
    {
        await _shop.Admin.CreateProductAsync(NewProduct() with { Category = "Toys" });

        Assert.Contains(await _shop.Catalog.GetCategoriesAsync(), category => category.Name == "Toys");
    }

    [Theory]
    [InlineData("Bad Id")]
    [InlineData("UPPER_case")]
    [InlineData("")]
    public async Task Product_ids_must_be_slugs(string id) =>
        await Assert.ThrowsAsync<ShopException>(() => _shop.Admin.CreateProductAsync(NewProduct(id)));

    [Fact]
    public async Task Duplicate_product_ids_conflict()
    {
        var exception = await Assert.ThrowsAsync<ShopException>(() => _shop.Admin.CreateProductAsync(NewProduct("ceramic-mug")));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task Negative_prices_and_stock_are_rejected()
    {
        await Assert.ThrowsAsync<ShopException>(() => _shop.Admin.CreateProductAsync(NewProduct() with { Price = -1 }));
        await Assert.ThrowsAsync<ShopException>(() => _shop.Admin.CreateProductAsync(NewProduct() with { Stock = -1 }));
        await Assert.ThrowsAsync<ShopException>(() => _shop.Admin.UpdateStockAsync("ceramic-mug", -1));
    }

    [Fact]
    public async Task Unused_products_are_deleted_but_ordered_products_are_only_deactivated()
    {
        await _shop.Admin.CreateProductAsync(NewProduct());

        Assert.True(await _shop.Admin.DeleteProductAsync("new-item"));
        Assert.False(await _shop.Admin.DeleteProductAsync("field-notebook"));
        Assert.False((await _shop.Admin.GetProductAsync("field-notebook")).IsActive);
    }

    [Fact]
    public async Task Stock_update_is_visible_immediately()
    {
        await _shop.Admin.UpdateStockAsync("canvas-tote", 4);

        Assert.Equal(4, (await _shop.Catalog.GetProductAsync("canvas-tote", null)).Stock);
    }

    [Fact]
    public async Task Coupons_can_be_created_updated_and_deleted()
    {
        var request = new SaveCouponRequest("spring25", "Spring", CouponType.Percentage, 25, 0, null, null);

        var created = await _shop.Admin.SaveCouponAsync(request, create: true);
        await _shop.Admin.SaveCouponAsync(request with { IsActive = false }, create: false);
        var coupons = await _shop.Admin.GetCouponsAsync();
        await _shop.Admin.DeleteCouponAsync("SPRING25");

        Assert.Equal("SPRING25", created.Code);
        Assert.False(coupons.Single(coupon => coupon.Code == "SPRING25").IsActive);
        Assert.DoesNotContain(await _shop.Admin.GetCouponsAsync(), coupon => coupon.Code == "SPRING25");
    }

    [Fact]
    public async Task Invalid_coupons_are_rejected()
    {
        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Admin.SaveCouponAsync(new SaveCouponRequest("X", "short code", CouponType.Percentage, 10, 0, null, null), true));
        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Admin.SaveCouponAsync(new SaveCouponRequest("TOOBIG", "x", CouponType.Percentage, 150, 0, null, null), true));
        await Assert.ThrowsAsync<ShopException>(() =>
            _shop.Admin.SaveCouponAsync(new SaveCouponRequest("WELCOME10", "duplicate", CouponType.Percentage, 5, 0, null, null), true));
    }

    [Fact]
    public async Task Dashboard_summarizes_orders_revenue_and_stock()
    {
        var dashboard = await _shop.Admin.GetDashboardAsync();

        Assert.Equal(3, dashboard.TotalOrders);
        Assert.Equal(0, dashboard.OpenOrders);
        Assert.True(dashboard.Revenue > 0);
        Assert.Equal(16, dashboard.ProductCount);
        Assert.Equal(2, dashboard.OutOfStockCount);
        Assert.Contains(dashboard.LowStockProducts, product => product.Id == "canvas-tote");
        Assert.Equal(2, dashboard.CustomerCount);
    }
}
