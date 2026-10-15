using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A drop-down select (<c>MudSelect</c>). MudBlazor puts <c>data-testid</c> on a hidden input inside the control and
/// renders the options in a popover at the end of the page body, outside the control.
/// </summary>
public sealed class Select(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    private ILocator OpenPopover => Page.Locator(".mud-popover-open");

    protected override ILocator ResolveControlRoot(ILocator matched)
        => matched.Locator("xpath=ancestor::div[contains(concat(' ', normalize-space(@class), ' '), ' mud-input-control ')][1]");

    /// <summary>
    /// Opens the select, picks the option with exactly the given text and waits until the popover has closed again.
    /// </summary>
    public async Task SelectOption(string optionText)
    {
        Trace($"Selecting option '{optionText}'.");
        await Locator.Locator(".mud-input").ClickAsync();
        await OpenPopover.GetByText(optionText, new() { Exact = true }).ClickAsync();
        await Assertions.Expect(OpenPopover).ToHaveCountAsync(0);
    }

    /// <summary>
    /// The text of the currently selected option as displayed by the control.
    /// </summary>
    public Task<string> GetSelectedText() => Locator.Locator("div.mud-input-slot.mud-select-input").InnerTextAsync();
}
