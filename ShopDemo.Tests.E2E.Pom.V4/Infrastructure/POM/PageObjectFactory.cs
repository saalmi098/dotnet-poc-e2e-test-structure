using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Entry point for creating fully bound page objects.
/// </summary>
/// <remarks>
/// Bindable types must expose exactly one public constructor with signature
/// <c>(IPage page, ILocator? baseLocator = null)</c>.
/// </remarks>
public sealed class PageObjectFactory
{
    /// <summary>
    /// Creates a <see cref="PageObject"/>, binds its annotated properties, and waits for required locators.
    /// </summary>
    public async Task<T> Create<T>(IPage page, ILocator? baseLocator = null) where T : PageObject
    {
        ArgumentNullException.ThrowIfNull(page);

        var binder = new ComponentBinder(page);
        return (T)await binder.CreateAndBind(typeof(T), baseLocator, waitForRequiredLocators: true);
    }
}
