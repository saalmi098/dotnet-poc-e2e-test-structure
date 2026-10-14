using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// Base class for leaf UI controls (button, text field, select, ...). A control wraps the locator that matched its
/// root element and encapsulates the interaction behaviour of that control.
/// </summary>
/// <remarks>
/// Deliberately does not implement <see cref="ILocator"/>: Playwright's <c>Expect(ILocator)</c> casts to its internal
/// locator class, so any wrapper would fail at runtime. Use <see cref="Expect"/> or <see cref="Locator"/> instead.
/// </remarks>
public abstract class Control(IPage page, ILocator? baseLocator = null) : Component(page, baseLocator)
{
    /// <summary>
    /// The locator of this control's root element. Escape hatch for Playwright APIs that need an <see cref="ILocator"/>.
    /// </summary>
    public ILocator Locator => BaseLocator
        ?? throw new InvalidOperationException($"Control '{GetType().Name}' has no root locator; it must be bound as a property of a page object.");

    /// <summary>
    /// Starts a Playwright assertion on this control's root element.
    /// </summary>
    public ILocatorAssertions Expect() => Assertions.Expect(Locator);
}
