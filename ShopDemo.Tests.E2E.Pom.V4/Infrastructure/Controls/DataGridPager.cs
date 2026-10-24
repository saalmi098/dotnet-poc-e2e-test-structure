using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// The pager of a <c>MudDataGrid</c> (<c>MudDataGridPager</c>): range caption such as "1-10 of 25",
/// first/previous/next/last buttons and the rows-per-page select.
/// </summary>
public sealed class DataGridPager(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    private Select RowsPerPageSelect => new(Page, Locator.Locator(".mud-table-pagination-select input"));

    /// <summary>
    /// The range caption, e.g. "1-10 of 25". The pager has two captions ("Rows per page:" and the range); this is the range.
    /// </summary>
    public ILocator Caption => Locator.Locator(".mud-table-pagination-caption").Last;

    public Task GoToFirstPage() => GoTo("First page");

    public Task GoToPreviousPage() => GoTo("Previous page");

    public Task GoToNextPage() => GoTo("Next page");

    public Task GoToLastPage() => GoTo("Last page");

    public async Task SetRowsPerPage(int rowsPerPage)
    {
        Trace($"Setting rows per page to {rowsPerPage}.");
        await RowsPerPageSelect.SelectOption(rowsPerPage.ToString());
    }

    /// <summary>
    /// Clicks a navigation button and waits until the range caption has changed, i.e. the new page is displayed.
    /// </summary>
    private Task GoTo(string buttonLabel) => TraceInteraction(
        "Click",
        async () =>
        {
            var previousCaption = (await Caption.InnerTextAsync()).Trim();
            Trace($"Clicking '{buttonLabel}' on page '{previousCaption}'.");
            await Locator.GetByRole(AriaRole.Button, new() { Name = buttonLabel }).ClickAsync();
            await Assertions.Expect(Caption).Not.ToHaveTextAsync(previousCaption);
        },
        new Dictionary<string, object?> { ["ui.button.label"] = buttonLabel });
}
