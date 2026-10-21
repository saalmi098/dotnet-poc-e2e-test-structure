using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Base class for page objects and nested sections using the Page Object Model. These objects share a browser page and may have a parent scope.
/// </summary>
public abstract class PageObject(IPage page, ILocator? baseLocator = null) : IBindableComponent
{
    private readonly PageBindingContext _bindingContext = new(page, baseLocator);
    private readonly List<IBindableComponent> _requiredChildren = [];

    /// <summary>
    /// The Playwright browser page used to interact with the application.
    /// </summary>
    protected IPage Page => _bindingContext.Page;

    /// <summary>
    /// Scope used when binding this page object's locators; null means the whole page.
    /// </summary>
    protected ILocator? BaseLocator => _bindingContext.BaseLocator;

    internal void AddRequiredChild(IBindableComponent child) => _requiredChildren.Add(child);

    public void AddRequiredLocator(ILocator locator) => _bindingContext.AddRequiredLocator(locator);

    /// <summary>
    /// Reports whether this page object and its required child components are currently ready.
    /// </summary>
    public async Task<bool> IsReady()
    {
        // checks required locators to be visible
        if (!await _bindingContext.IsReady())
        {
            return false;
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

    /// <summary>
    /// Creates and binds a child page object, optionally limiting its locators to a component scope.
    /// </summary>
    protected Task<T> Create<T>(ILocator? scopedLocator = null) where T : PageObject
        => new PageObjectFactory().Create<T>(Page, scopedLocator);
}
