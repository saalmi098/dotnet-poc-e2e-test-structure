namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DataTestIdAttribute(string testId) : LocatorMetadataAttribute(true)
{
    public string TestId { get; } = testId;

    internal override string Describe() => $"data-testid='{TestId}'";
}
