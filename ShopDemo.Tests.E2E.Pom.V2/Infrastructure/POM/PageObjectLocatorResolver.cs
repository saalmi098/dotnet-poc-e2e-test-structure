using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

internal static class PageObjectLocatorResolver
{
    internal static LocatorMetadataAttribute? GetBindingAttribute(Type objectType, PropertyInfo property)
    {
        var isAutoBoundProperty =
            typeof(ILocator).IsAssignableFrom(property.PropertyType)
            || typeof(PageObject).IsAssignableFrom(property.PropertyType);
        var attributes = property
            .GetCustomAttributes<LocatorMetadataAttribute>(inherit: true)
            .ToArray();

        if (!isAutoBoundProperty)
        {
            if (attributes.Length > 0)
            {
                throw new PageObjectBindingException(
                    $"Property '{objectType.Name}.{property.Name}' has a locator attribute but is not an ILocator or PageObject property.");
            }

            return null;
        }

        if (attributes.Length != 1)
        {
            throw new PageObjectBindingException(
                $"Property '{objectType.Name}.{property.Name}' must have exactly one '{nameof(LocatorMetadataAttribute)}' attribute! " +
                $"Found {attributes.Length}.");
        }

        return attributes[0];
    }

    internal static ILocator Resolve(IPage page, ILocator? baseLocator, LocatorMetadataAttribute attribute)
        => attribute switch
        {
            DataTestIdAttribute testIdAttribute => ResolveDataTestIdAttribute(page, baseLocator, testIdAttribute),
            LocatorAttribute locatorAttribute => ResolveLocatorAttribute(page, baseLocator, locatorAttribute),
            _ => throw new PageObjectBindingException("A Locator selector cannot be empty or unsupported.")
        };

    private static ILocator ResolveDataTestIdAttribute(
        IPage page,
        ILocator? baseLocator,
        DataTestIdAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.TestId))
        {
            throw new PageObjectBindingException("A DataTestId locator cannot be empty.");
        }

        return baseLocator is null
            ? page.GetByTestId(attribute.TestId)
            : baseLocator.GetByTestId(attribute.TestId);
    }

    private static ILocator ResolveLocatorAttribute(
        IPage page,
        ILocator? baseLocator,
        LocatorAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Selector))
        {
            throw new PageObjectBindingException("A Locator selector cannot be empty or unsupported.");
        }

        return attribute.Kind switch
        {
            // TODO: test if those all work (not only within the constructed example in PageObjectFactoryTests.cs, but with the running application in the browser)
            LocatorKind.Role => ResolveByRole(page, baseLocator, attribute),
            LocatorKind.Text => ResolveByText(page, baseLocator, attribute),
            LocatorKind.Css => ResolveByCss(page, baseLocator, attribute.Selector),
            LocatorKind.XPath => ResolveByXPath(page, baseLocator, attribute.Selector),
            _ => throw new PageObjectBindingException(
                $"Unsupported locator kind '{attribute.Kind}' on selector '{attribute.Selector}'.")
        };
    }

    private static ILocator ResolveByRole(IPage page, ILocator? baseLocator, LocatorAttribute attribute)
    {
        if (!Enum.TryParse<AriaRole>(attribute.Selector, ignoreCase: true, out var role))
        {
            throw new PageObjectBindingException($"'{attribute.Selector}' is not a valid Playwright ARIA role.");
        }

        if (baseLocator is not null)
        {
            return baseLocator.GetByRole(role, new LocatorGetByRoleOptions
            {
                Name = attribute.Name,
                Exact = attribute.Exact
            });
        }

        return page.GetByRole(role, new PageGetByRoleOptions
        {
            Name = attribute.Name,
            Exact = attribute.Exact
        });
    }

    private static ILocator ResolveByText(IPage page, ILocator? baseLocator, LocatorAttribute attribute)
    {
        if (baseLocator is not null)
        {
            return baseLocator.GetByText(attribute.Selector, new LocatorGetByTextOptions
            {
                Exact = attribute.Exact
            });
        }

        return page.GetByText(attribute.Selector, new PageGetByTextOptions
        {
            Exact = attribute.Exact
        });
    }

    private static ILocator ResolveByCss(IPage page, ILocator? baseLocator, string selector) =>
        baseLocator is null
            ? page.Locator($"css={selector}")
            : baseLocator.Locator($"css={selector}");

    private static ILocator ResolveByXPath(IPage page, ILocator? baseLocator, string selector) =>
        baseLocator is null
            ? page.Locator($"xpath={selector}")
            : baseLocator.Locator($"xpath={selector}");
}
