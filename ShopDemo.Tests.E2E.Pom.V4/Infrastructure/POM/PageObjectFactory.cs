using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Otel;

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
    /// <remarks>
    /// Traced as a "{PageObjectType}.Bind" span: binding recursively waits for every required locator to
    /// become visible (MudBlazor render + any data load behind it), which is often where the real time
    /// between user interactions goes. Covers both the initial page object a test creates and any nested
    /// one created later (e.g. switching tabs re-binds a tab's page object), since both go through here.
    /// </remarks>
    public Task<T> Create<T>(IPage page, ILocator? baseLocator = null) where T : PageObject
    {
        ArgumentNullException.ThrowIfNull(page);

        var testRoot = UserInteractionTelemetry.GetTestRootActivity(page);
        return UserInteractionTracer.TraceAsync(
            typeof(T).Name,
            "Bind",
            page.Url,
            async () =>
            {
                var binder = new ComponentBinder(page);
                return (T)await binder.CreateAndBind(typeof(T), baseLocator, waitForRequiredLocators: true);
            },
            testRoot);
    }
}
