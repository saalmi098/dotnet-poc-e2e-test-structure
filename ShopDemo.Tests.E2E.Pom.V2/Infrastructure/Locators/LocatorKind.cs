namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

/// <summary>
/// Selector strategies supported by <see cref="LocatorAttribute"/>.
/// </summary>
public enum LocatorKind
{
    /// <summary>
    /// Finds an element by its ARIA role, optionally filtered by accessible name.
    /// </summary>
    Role,

    /// <summary>
    /// Finds an element by text; exact matching is controlled by <see cref="LocatorAttribute.Exact"/>.
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
