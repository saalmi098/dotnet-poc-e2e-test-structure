using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ProductDetailsInventoryTab(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "product-inventory-id")]
    public required ILocator ProductId { get; init; }

    [Locator(LocatorKind.DataTestId, "product-inventory-stock")]
    public required ILocator Stock { get; init; }

    [Locator(LocatorKind.DataTestId, "product-inventory-availability")]
    public required ILocator Availability { get; init; }

    [Locator(LocatorKind.DataTestId, "product-inventory-grid")]
    public required DataGrid Grid { get; init; }

    [Locator(LocatorKind.DataTestId, "product-inventory-pager")]
    public required DataGridPager Pager { get; init; }

    /// <summary>
    /// The column headers of the inventory grid.
    /// </summary>
    public ILocator ColumnHeaders => Grid.ColumnHeaders;

    /// <summary>
    /// The inventory rows of the currently displayed page.
    /// </summary>
    public ILocator Rows => Grid.Rows;

    /// <summary>
    /// The product id cells of the currently displayed rows.
    /// </summary>
    public ILocator ProductIdCells => Grid.GetColumnCells("Product ID");

    /// <summary>
    /// The first row's product id cell.
    /// </summary>
    public ILocator FirstRowProductId => Grid.GetCell(0, "Product ID");

    /// <summary>
    /// The displayed range, e.g. "1-10 of 25".
    /// </summary>
    public ILocator PageRange => Pager.Caption;

    public Task GoToFirstPage() => Pager.GoToFirstPage();

    public Task GoToNextPage() => Pager.GoToNextPage();

    public Task GoToLastPage() => Pager.GoToLastPage();

    public Task ShowRowsPerPage(int rowsPerPage) => Pager.SetRowsPerPage(rowsPerPage);
}
