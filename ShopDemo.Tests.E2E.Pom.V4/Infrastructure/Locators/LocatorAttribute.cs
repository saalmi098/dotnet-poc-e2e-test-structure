namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;

/// <summary>
/// Identifies an auto-bound UI property by test ID, role, text, CSS, or XPath.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LocatorAttribute(LocatorKind kind, string selector) : Attribute
{
    /// <summary>
    /// The strategy used to interpret <see cref="Selector"/>.
    /// </summary>
    public LocatorKind Kind { get; } = kind;

    /// <summary>
    /// The value to locate: test ID, ARIA role, text, CSS selector, or XPath expression, depending on <see cref="Kind"/>.
    /// </summary>
    public string Selector { get; } = selector;

    /// <summary>
    /// Whether the locator is required to become visible during binding and included in readiness checks.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// The exact accessible name to match for a <see cref="LocatorKind.Role"/> locator. Not supported by other kinds.
    /// </summary>
    public string? Name { get; set; }

    internal string Describe()
    {
        if (Kind == LocatorKind.DataTestId)
        {
            return $"data-testid='{Selector}'";
        }

        var name = Name is null ? string.Empty : $", name='{Name}'";
        return $"{Kind}('{Selector}'{name})";
    }
}
