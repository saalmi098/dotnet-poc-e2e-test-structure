using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A data grid (<c>MudDataGrid</c>). Rows and cells are addressed by position and column title, so the page under
/// test does not need a test id on every cell. Only the currently displayed page of rows is visible to it.
/// </summary>
/// <remarks>
/// Exposes locators rather than snapshot values so tests can use Playwright's retrying assertions, e.g.
/// <c>Expect(grid.Rows).ToHaveCountAsync(10)</c>.
/// </remarks>
public sealed class DataGrid(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    /// <summary>
    /// The column titles, one per column. Targets the title span, because the whole header cell also contains
    /// MudBlazor's sort-index text.
    /// </summary>
    public ILocator ColumnHeaders => Locator.Locator("thead th .sortable-column-header");

    /// <summary>
    /// The rows of the currently displayed page.
    /// </summary>
    public ILocator Rows => Locator.Locator("tbody tr.mud-table-row");

    /// <summary>
    /// The cells of the given column (matched by its title) across the displayed rows.
    /// </summary>
    public ILocator GetColumnCells(string columnTitle) => Rows.Locator($"td[data-label='{columnTitle}']");

    /// <summary>
    /// The cell in the given column of the displayed row (zero-based).
    /// </summary>
    public ILocator GetCell(int rowIndex, string columnTitle) => Rows.Nth(rowIndex).Locator($"td[data-label='{columnTitle}']");
}
