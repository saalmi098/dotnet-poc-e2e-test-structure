using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Provides the page and locator scope for a page object or control and tracks its required locators for readiness checks.
/// </summary>
/// <param name="page">The Playwright page used for this object.</param>
/// <param name="baseLocator">Optional parent locator that scopes the object's bindings; null scopes them to the whole page./// </param>
internal sealed class PageBindingContext(IPage page, ILocator? baseLocator = null)
{
    private readonly List<ILocator> _requiredLocators = [];

    /// <summary>
    /// Parent locator used to scope bindings, or null when bindings use the whole page (default).
    /// </summary>
    public ILocator? BaseLocator { get; } = baseLocator;

    /// <summary>
    /// Playwright page used by the page object or control.
    /// </summary>
    public IPage Page { get; } = page;

    /// <summary>
    /// Adds a locator that must be visible for this object to be ready.
    /// </summary>
    public void AddRequiredLocator(ILocator locator) => _requiredLocators.Add(locator);

    /// <summary>
    /// Checks whether every registered required locator is currently visible.
    /// This check does not wait for locators to become visible.
    /// </summary>
    public async Task<bool> IsReady()
    {
        foreach (var locator in _requiredLocators)
        {
            if (!await locator.IsVisibleAsync())
            {
                return false;
            }
        }

        return true;
    }
}
