using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class ProductDetailsInventoryTab(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("product-inventory-id")]
    public ILocator ProductId { get; private set; } = null!;

    [DataTestId("product-inventory-stock")]
    public ILocator Stock { get; private set; } = null!;

    [DataTestId("product-inventory-availability")]
    public ILocator Availability { get; private set; } = null!;

    [DataTestId("product-inventory-grid")]
    public DataGrid Grid { get; private set; } = null!;

    [DataTestId("product-inventory-pager")]
    public DataGridPager Pager { get; private set; } = null!;

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
