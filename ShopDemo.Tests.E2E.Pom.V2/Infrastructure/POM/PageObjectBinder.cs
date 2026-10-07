using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

internal static class PageObjectBinder
{
    internal static async Task Bind(
        PageObject instance,
        IPage page,
        ILocator? baseLocator,
        PageObjectFactory factory,
        bool waitForRequiredLocators)
    {
        var objectType = instance.GetType();

        foreach (var property in objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var isLocator = typeof(ILocator).IsAssignableFrom(property.PropertyType);
            var isNestedPageObject = typeof(PageObject).IsAssignableFrom(property.PropertyType);
            var attributes = property.GetCustomAttributes<LocatorMetadataAttribute>(inherit: true).ToArray();

            if (!isLocator && !isNestedPageObject)
            {
                if (attributes.Length > 0)
                {
                    throw new PageObjectBindingException(
                        $"{objectType.Name}.{property.Name} has a locator attribute but is not an ILocator or PageObject property.");
                }

                continue;
            }

            var attribute = LocatorAttributeValidation.GetSingleAttribute(objectType, property);
            if (property.GetMethod is null || property.SetMethod is null)
            {
                throw new PageObjectBindingException(
                    $"{objectType.Name}.{property.Name} must have a getter and setter to be auto-bound.");
            }

            ILocator locator;
            try
            {
                locator = Resolve(page, baseLocator, attribute);
            }
            catch (Exception exception) when (exception is PlaywrightException or ArgumentException or InvalidOperationException)
            {
                throw new PageObjectBindingException(
                    $"Could not resolve {objectType.Name}.{property.Name} using selector {attribute.Describe()}.",
                    exception);
            }

            var required = attribute.Required;
            if (required && waitForRequiredLocators)
            {
                try
                {
                    await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                }
                catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
                {
                    throw new PageObjectBindingException(
                        $"Required locator for {objectType.Name}.{property.Name} was not visible. " +
                        $"Selector: {attribute.Describe()}.",
                        exception);
                }
            }

            if (isLocator)
            {
                property.SetValue(instance, locator);
                if (required)
                {
                    instance.AddRequiredLocator(locator);
                }

                continue;
            }

            var child = await factory.Create(
                property.PropertyType,
                page,
                locator,
                waitForRequiredLocators: required && waitForRequiredLocators);
            property.SetValue(instance, child);
            if (required)
            {
                instance.AddRequiredChild(child);
            }
        }
    }

    private static ILocator Resolve(IPage page, ILocator? baseLocator, LocatorMetadataAttribute attribute)
    {
        if (attribute is DataTestIdAttribute testIdAttribute)
        {
            if (string.IsNullOrWhiteSpace(testIdAttribute.TestId))
            {
                throw new PageObjectBindingException("A DataTestId locator cannot be empty.");
            }

            return baseLocator is null
                ? page.GetByTestId(testIdAttribute.TestId)
                : baseLocator.GetByTestId(testIdAttribute.TestId);
        }

        if (attribute is not LocatorAttribute locatorAttribute
            || string.IsNullOrWhiteSpace(locatorAttribute.Selector))
        {
            throw new PageObjectBindingException("A Locator selector cannot be empty or unsupported.");
        }

        switch (locatorAttribute.Kind)
        {
            case LocatorKind.Role:
                if (!Enum.TryParse<AriaRole>(locatorAttribute.Selector, ignoreCase: true, out var role))
                {
                    throw new PageObjectBindingException(
                        $"'{locatorAttribute.Selector}' is not a valid Playwright ARIA role.");
                }

                return baseLocator is null
                    ? page.GetByRole(role, new PageGetByRoleOptions
                    {
                        Name = locatorAttribute.Name,
                        Exact = locatorAttribute.Exact
                    })
                    : baseLocator.GetByRole(role, new LocatorGetByRoleOptions
                    {
                        Name = locatorAttribute.Name,
                        Exact = locatorAttribute.Exact
                    });

            case LocatorKind.Text:
                return baseLocator is null
                    ? page.GetByText(locatorAttribute.Selector, new PageGetByTextOptions
                    {
                        Exact = locatorAttribute.Exact
                    })
                    : baseLocator.GetByText(locatorAttribute.Selector, new LocatorGetByTextOptions
                    {
                        Exact = locatorAttribute.Exact
                    });

            case LocatorKind.Css:
                return baseLocator is null
                    ? page.Locator($"css={locatorAttribute.Selector}")
                    : baseLocator.Locator($"css={locatorAttribute.Selector}");

            case LocatorKind.XPath:
                return baseLocator is null
                    ? page.Locator($"xpath={locatorAttribute.Selector}")
                    : baseLocator.Locator($"xpath={locatorAttribute.Selector}");

            default:
                throw new PageObjectBindingException(
                    $"Unsupported locator kind '{locatorAttribute.Kind}' on selector '{locatorAttribute.Selector}'.");
        }
    }
}
