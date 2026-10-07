namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

[AttributeUsage(AttributeTargets.Property)]
public abstract class LocatorMetadataAttribute(bool required) : Attribute
{
    public bool Required { get; set; } = required;

    internal abstract string Describe();
}
