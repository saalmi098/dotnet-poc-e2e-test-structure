namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

/// <summary>
/// Identifies an auto-bound UI property with a role, text, CSS, or XPath selector.
/// </summary>
/// <remarks>
/// For role selectors, <c>Selector</c> is an ARIA role, <c>Name</c> filters by accessible name,
/// and <c>Exact</c> controls exact name matching. For text selectors, <c>Selector</c> is the text
/// and <c>Exact</c> controls exact matching. CSS and XPath use <c>Selector</c> as the query.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LocatorAttribute(LocatorKind kind, string selector) : LocatorMetadataAttribute(true)
{
    public LocatorKind Kind { get; } = kind;

    public string Selector { get; } = selector;

    public string? Name { get; set; }

    public bool Exact { get; set; }

    internal override string Describe()
    {
        var name = Name is null ? string.Empty : $", name='{Name}'";
        return $"{Kind}('{Selector}'{name})";
    }
}
