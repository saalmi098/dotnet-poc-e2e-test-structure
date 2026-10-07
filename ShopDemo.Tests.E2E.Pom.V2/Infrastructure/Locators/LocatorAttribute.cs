namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

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
