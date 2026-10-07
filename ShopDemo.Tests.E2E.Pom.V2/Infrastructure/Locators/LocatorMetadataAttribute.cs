using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

/// <summary>
/// Base metadata describing how the <see cref="PageObjectBinder"/> finds a UI property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public abstract class LocatorMetadataAttribute(bool required) : Attribute
{
    /// <summary>
    /// Determines whether the locator is required during binding and readiness checks.
    /// </summary>
    /// <remarks>
    /// Built-in locator attributes are required by default. Required locators are waited for during
    /// creation and included in the owning object's readiness (<see cref="PageObject.IsReady"/>).
    /// Optional locators are still assigned, but skip the initial wait and the parent's readiness requirement.
    /// </remarks>
    public bool Required { get; set; } = required;

    internal abstract string Describe();
}
