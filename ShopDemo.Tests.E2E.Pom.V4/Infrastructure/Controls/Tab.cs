using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A tab header of a <c>MudTabs</c> component. MudBlazor renders the tab as a <c>div[role=tab]</c>; a test id placed
/// in the panel's <c>TabContent</c> ends up on an element inside it, so the root is resolved to the enclosing tab.
/// </summary>
public sealed class Tab(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    protected override ILocator ResolveRoot(ILocator matched)
        => matched.Locator("xpath=ancestor-or-self::*[@role='tab'][1]");

    /// <summary>
    /// Activates the tab and waits until MudBlazor marks it as selected.
    /// </summary>
    public async Task Select()
    {
        Trace("Selecting tab.");
        await Locator.ClickAsync();
        await Expect().ToHaveAttributeAsync("aria-selected", "true");
    }

    public async Task<bool> IsSelected() => await Locator.GetAttributeAsync("aria-selected") == "true";
}
