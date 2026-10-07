namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

/// <summary>
/// Identifies an auto-bound UI property by its <c>data-testid</c> value.
/// </summary>
/// <remarks>
/// Properties using this attribute are required by default.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DataTestIdAttribute(string testId) : LocatorMetadataAttribute(true)
{
    public string TestId { get; } = testId;

    internal override string Describe() => $"data-testid='{TestId}'";
}
