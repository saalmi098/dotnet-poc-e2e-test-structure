using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class CatalogPageTests : ShopDemoPageTestV4<CatalogPage>
{
    protected override string PageUrl => "/";

    [Fact]
    public async Task Catalog_can_be_filtered_by_category_through_the_select_control()
    {
        await Model.ProductCards.First.WaitForAsync();
        var allProducts = await Model.ProductCards.CountAsync();

        await Model.FilterByCategory("Home");

        Assert.Equal("Home", await Model.Category.GetSelectedText());
        await Model.Category.Expect().ToContainTextAsync("Home");
        var homeProducts = await Model.ProductCards.CountAsync();
        Assert.InRange(homeProducts, 1, allProducts - 1);

        await Model.FilterByCategory("All categories");

        await Model.Category.Expect().ToContainTextAsync("All categories");
        await Assertions.Expect(Model.ProductCards).ToHaveCountAsync(allProducts);
    }

    [Fact]
    public async Task Catalog_can_be_searched_through_the_text_field_control()
    {
        await Model.ProductCards.First.WaitForAsync();

        await Model.SearchFor("Tote");

        await Expect(Model.GetProductCard(E2ETestSettings.CanvasToteId)).ToBeVisibleAsync();
        Assert.Equal("Tote", await Model.Search.GetValue());
        await Expect(Model.ProductCards).ToHaveCountAsync(1);

        await Model.SearchFor("no product has this name");

        await Expect(Model.ProductCards).ToHaveCountAsync(0);
    }
}
