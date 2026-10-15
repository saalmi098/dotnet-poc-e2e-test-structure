using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Base class for page objects and nested sections using the Page Object Model. These objects share a browser page and may have a parent scope.
/// </summary>
public abstract class PageObject(IPage page, ILocator? baseLocator = null)
    : Component(page, baseLocator)
{
    private readonly List<Component> _requiredChildren = [];

    internal void AddRequiredChild(Component child) => _requiredChildren.Add(child);

    /// <summary>
    /// Reports whether this page object and its required child components are currently ready.
    /// </summary>
    public override async Task<bool> IsReady()
    {
        if (!await base.IsReady())
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
