using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Shared browser-page and locator-scope context for page objects and controls.
/// </summary>
public abstract class Component(IPage page, ILocator? baseLocator = null)
{
    private readonly List<ILocator> _requiredLocators = [];

    /// <summary>
    /// The Playwright browser page used to interact with the application.
    /// </summary>
    protected IPage Page { get; } = page;

    /// <summary>
    /// Scope used when binding this component's locators; null means the whole page (default).
    /// For nested components this is the locator matched by the parent's property.
    /// </summary>
    protected ILocator? BaseLocator { get; } = baseLocator;

    internal void AddRequiredLocator(ILocator locator) => _requiredLocators.Add(locator);

    /// <summary>
    /// Reports whether this component's required locators are currently visible.
    /// </summary>
    public virtual async Task<bool> IsReady()
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
