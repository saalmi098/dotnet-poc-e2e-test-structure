namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;

/// <summary>
/// Selector strategies supported by <see cref="LocatorAttribute"/>.
/// </summary>
public enum LocatorKind
{
    /// <summary>
    /// Finds an element by its <c>data-testid</c> value.
    /// </summary>
    DataTestId,

    /// <summary>
    /// Finds an element by its ARIA role, optionally filtered by an exact accessible name.
    /// </summary>
    Role,

    /// <summary>
    /// Finds an element by exact text.
    /// </summary>
    Text,

    /// <summary>
    /// Finds elements using a CSS selector.
    /// </summary>
    Css,

    /// <summary>
    /// Finds elements using an XPath expression.
    /// </summary>
    XPath
}
