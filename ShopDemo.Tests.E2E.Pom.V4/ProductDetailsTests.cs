using ShopDemo.Tests.E2E.Pom.V4.POM;
using ShopDemo.Tests.E2E.Shared;
using System.Diagnostics;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class ProductDetailsTests(ITestOutputHelper output) : ShopDemoPageTestV4<CatalogPage>
{
    protected override string PageUrl => "/";

    [Fact]
    public async Task Product_details_tabs_bind_fields_after_parent_activates_them()
    {
        var product = await Model.OpenProductDetails(E2ETestSettings.CanvasToteId);

        // The Tab control resolves its root to the enclosing role=tab element of the labelled span.
        await product.OverviewTab.Expect().ToHaveAttributeAsync("role", "tab");
        await product.InventoryTab.Expect().ToHaveAttributeAsync("role", "tab");

        var overview = await product.SwitchToOverviewTab();
        Assert.Equal("Market Canvas Tote", await overview.Name.InnerTextAsync());
        Assert.False(string.IsNullOrWhiteSpace(await overview.Description.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Category.InnerTextAsync()));
        Assert.False(string.IsNullOrWhiteSpace(await overview.Price.InnerTextAsync()));
        Assert.True(await overview.IsReady());

        Log("Starting Inventory tab activation and page-object binding.");

        var inventoryLoad = Stopwatch.StartNew();
        var inventoryTask = product.SwitchToInventoryTab();
        await Page.GetByTestId("product-inventory-loading")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        Log($"Loading indicator is visible after {inventoryLoad.Elapsed}; inventory page-object task completed: {inventoryTask.IsCompleted}.");

        var inventory = await inventoryTask;
        Log($"Inventory page object is ready after {inventoryLoad.Elapsed}.");

        Assert.True(
            inventoryLoad.Elapsed >= TimeSpan.FromSeconds(4.5),
            $"Inventory loaded too quickly: {inventoryLoad.Elapsed}.");

        await Expect(inventory.ProductId).ToHaveTextAsync(E2ETestSettings.CanvasToteId);
        await Expect(inventory.Stock).ToHaveTextAsync("0");
        await Expect(inventory.Availability).ToHaveTextAsync("Out of stock");
        await Expect(inventory.Rows).ToHaveCountAsync(10);
        await inventory.Pager.Expect().ToBeVisibleAsync();
        Assert.True(await inventory.IsReady());
    }

    [Fact]
    public async Task Product_inventory_grid_can_be_read_and_paged_through_grid_controls()
    {
        var product = await Model.OpenProductDetails(E2ETestSettings.CanvasToteId);
        await product.SwitchToOverviewTab();
        await product.OverviewTab.Expect().ToHaveAttributeAsync("aria-selected", "true");

        var inventory = await product.SwitchToInventoryTab();
        await product.InventoryTab.Expect().ToHaveAttributeAsync("aria-selected", "true");
        await product.OverviewTab.Expect().ToHaveAttributeAsync("aria-selected", "false");

        await Expect(inventory.ColumnHeaders).ToHaveTextAsync(["#", "Product ID", "Stock", "Availability"]);
        await Expect(inventory.Rows).ToHaveCountAsync(10);
        await Expect(inventory.FirstRowProductId).ToHaveTextAsync(E2ETestSettings.CanvasToteId);
        await Expect(inventory.PageRange).ToHaveTextAsync("1-10 of 25");

        await inventory.GoToNextPage();
        await Expect(inventory.PageRange).ToHaveTextAsync("11-20 of 25");
        await Expect(inventory.Rows).ToHaveCountAsync(10);

        await inventory.GoToLastPage();
        await Expect(inventory.PageRange).ToHaveTextAsync("21-25 of 25");
        await Expect(inventory.Rows).ToHaveCountAsync(5);

        await inventory.GoToFirstPage();
        await inventory.ShowRowsPerPage(25);
        await Expect(inventory.PageRange).ToHaveTextAsync("1-25 of 25");
        await Expect(inventory.ProductIdCells).ToHaveTextAsync(
            Enumerable.Repeat(E2ETestSettings.CanvasToteId, 25).ToArray());
    }

    private void Log(string message)
    {
        E2ETrace.Write(typeof(ProductDetailsTests), message, output.WriteLine);
    }
}
