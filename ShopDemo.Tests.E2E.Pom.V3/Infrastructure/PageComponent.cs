using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

/// <summary>
/// Base class for page/tab objects and nested components. Required and optional locators are
/// registered once, explicitly, in the constructor via <see cref="Require"/>/<see cref="Optional"/> —
/// there is no reflective discovery of attributed properties.
/// </summary>
public abstract class PageComponent
{
    private readonly List<(ILocator Locator, string? Description)> _required = [];
    private readonly List<PageComponent> _requiredChildren = [];

    /// <summary>
    /// Registers a locator as required for readiness and returns it wrapped as
    /// <see cref="Required{T}"/>. The description, when given, names the locator in
    /// <see cref="PageComponentNotReadyException"/> if it never becomes visible.
    /// </summary>
    protected Required<ILocator> Require(ILocator locator, string? description = null)
    {
        _required.Add((locator, description));
        return new Required<ILocator>(locator);
    }

    /// <summary>
    /// Wraps a locator that may legitimately be absent; it never blocks WaitReady()/IsReady().
    /// </summary>
    protected static Optional<ILocator> Optional(ILocator locator) => new(locator);

    /// <summary>
    /// Registers a nested component as required; its own required locators/children are checked
    /// recursively by WaitReady()/IsReady().
    /// </summary>
    protected T RequireChild<T>(T child) where T : PageComponent
    {
        _requiredChildren.Add(child);
        return child;
    }

    /// <summary>
    /// Constructs <typeparamref name="T"/> via its <see cref="IPageComponent{TSelf}"/> factory
    /// method, waits for its required locators/children, then returns it ready.
    /// </summary>
    public static async Task<T> Create<T>(IPage page, ILocator? baseLocator = null)
        where T : PageComponent, IPageComponent<T>
    {
        var instance = T.CreateInstance(page, baseLocator);
        await instance.WaitReady();
        return instance;
    }

    public async Task WaitReady()
    {
        foreach (var (locator, description) in _required)
        {
            try
            {
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            }
            catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
            {
                throw new PageComponentNotReadyException(
                    $"Required locator '{description ?? GetType().Name}' was not visible.",
                    exception);
            }
        }

        foreach (var child in _requiredChildren)
        {
            await child.WaitReady();
        }
    }

    public async Task<bool> IsReady()
    {
        foreach (var (locator, _) in _required)
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
