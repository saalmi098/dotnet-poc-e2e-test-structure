using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;

internal static class LocatorAttributeValidation
{
    internal static LocatorMetadataAttribute GetSingleAttribute(Type objectType, PropertyInfo property)
    {
        var attributes = property
            .GetCustomAttributes<LocatorMetadataAttribute>(inherit: true)
            .ToArray();

        if (attributes.Length != 1)
        {
            throw new PageObjectBindingException(
                $"{objectType.Name}.{property.Name} must have exactly one '{nameof(LocatorMetadataAttribute)}' attribute! Found {attributes.Length}.");
        }

        return attributes[0];
    }
}
