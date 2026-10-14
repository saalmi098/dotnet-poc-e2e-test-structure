using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Common base of everything the binder can construct: page objects (<see cref="PageObject"/>) and leaf UI controls.
/// Holds the browser page, the optional parent scope, and the readiness bookkeeping.
/// </summary>
public abstract class Component(IPage page, ILocator? baseLocator = null)
{
    private readonly List<ILocator> _requiredLocators = [];
    private readonly List<Component> _requiredChildren = [];

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

    internal void AddRequiredChild(Component child) => _requiredChildren.Add(child);

    /// <summary>
    /// Reports whether this component and its required children are currently ready (i.e. all required locators and children are visible).
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

        foreach (var child in _requiredChildren)
        {
            if (!await child.IsReady())
            {
                return false;
            }
        }

        return true;
    }
}
