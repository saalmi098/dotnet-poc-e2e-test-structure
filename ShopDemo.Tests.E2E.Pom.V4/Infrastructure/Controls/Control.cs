using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// Base class for leaf UI controls (button, text field, select, ...). A control wraps the element matched by its
/// locator attribute and encapsulates the interaction behaviour of that control.
/// </summary>
/// <remarks>
/// Deliberately does not implement <see cref="ILocator"/>: Playwright's <c>Expect(ILocator)</c> casts to its internal
/// locator class, so any wrapper would fail at runtime. Use <see cref="Expect"/> or <see cref="Locator"/> instead.
/// </remarks>
public abstract class Control(IPage page, ILocator? baseLocator = null) : Component(page, baseLocator)
{
    private ILocator? _root;

    /// <summary>
    /// The locator of this control's root element. Escape hatch for Playwright APIs that need an <see cref="ILocator"/>.
    /// </summary>
    public ILocator Locator => _root ??= ResolveRoot(
        BaseLocator ?? throw new InvalidOperationException(
            $"Control '{GetType().Name}' has no locator; it must be bound as an attributed property of a page object."));

    /// <summary>
    /// Starts a Playwright assertion on this control's root element.
    /// </summary>
    public ILocatorAssertions Expect() => Assertions.Expect(Locator); // TODO: not necessarily needed. Instead of writing e.g. `await control.Expect().ToBeVisibleAsync()`, we could just write `await Expect(control.Locator).ToBeVisibleAsync()`.

    /// <summary>
    /// Maps the element matched by the locator attribute to the control's root element. MudBlazor does not forward
    /// <c>data-testid</c> consistently (e.g. a select puts it on a hidden input), so controls override this to find
    /// the visible element. The binder waits for the resolved root, not for the matched element.
    /// </summary>
    protected virtual ILocator ResolveRoot(ILocator matched) => matched;

    /// <summary>
    /// Writes a trace line for an interaction with this control.
    /// </summary>
    protected void Trace(string message) => E2ETrace.Write(GetType(), message);
}
