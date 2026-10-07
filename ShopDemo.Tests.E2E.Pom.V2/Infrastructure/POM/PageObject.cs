using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

public abstract class PageObject(IPage page, ILocator? baseLocator = null)
{
    private readonly List<ILocator> _requiredLocators = [];
    private readonly List<PageObject> _requiredChildren = [];

    protected IPage Page { get; } = page;
    protected ILocator? BaseLocator { get; } = baseLocator;

    internal void AddRequiredLocator(ILocator locator) => _requiredLocators.Add(locator);

    internal void AddRequiredChild(PageObject child) => _requiredChildren.Add(child);

    protected Task<T> Create<T>(ILocator? scopedLocator = null) where T : PageObject
        => new PageObjectFactory().Create<T>(Page, scopedLocator);

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
