using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

/// <summary>
/// Base class for page objects and nested components using the Page Object Model. These objects share a browser page and may have a parent scope.
/// </summary>
public abstract class PageObject(IPage page, ILocator? baseLocator = null)
{
    private readonly List<ILocator> _requiredLocators = [];
    private readonly List<PageObject> _requiredChildren = [];

    /// <summary>
    /// The Playwright browser page used to interact with the application.
    /// </summary>
    protected IPage Page { get; } = page;

    /// <summary>
    /// Scope used when binding this object's locators; null means the whole page (default).
    /// This is typically set by the parent page object when creating a child component.
    /// </summary>
    protected ILocator? BaseLocator { get; } = baseLocator;

    internal void AddRequiredLocator(ILocator locator) => _requiredLocators.Add(locator);

    internal void AddRequiredChild(PageObject child) => _requiredChildren.Add(child);

    /// <summary>
    /// Creates and binds a child page object, optionally limiting its locators to a component scope.
    /// </summary>
    protected Task<T> Create<T>(ILocator? scopedLocator = null) where T : PageObject
        => new PageObjectFactory().Create<T>(Page, scopedLocator);

    /// <summary>
    /// Reports whether this object and its required child objects are currently ready (i.e. all required locators and child objects are visible).
    /// </summary>
    public async Task<bool> IsReady()
    {
        // TODO: check if the page is awaited to be ready when the page object is created (i.e. when the test navigates to the page)
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
