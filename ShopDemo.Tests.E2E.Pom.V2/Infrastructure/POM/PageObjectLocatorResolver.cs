using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

/// <summary>
/// Validates <see cref="LocatorMetadataAttribute"/> on page-object properties and resolves selectors to Playwright locators.
/// </summary>
internal static class PageObjectLocatorResolver
{
    /// <summary>
    /// Defines the property types that can be auto-bound by <see cref="PageObjectBinder"/>.
    /// </summary>
    private static Type[] SupportedAutoBoundPropertyTypes =>
    [
        typeof(ILocator),
        typeof(PageObject) // for nested page-object properties
    ];

    /// <summary>
    /// Returns the binding attribute for an auto-bound property, or null for a property without one.
    /// </summary>
    /// <remarks>
    /// Only properties carrying a locator metadata attribute are bound; all others are ignored and keep their
    /// default value. A bound property must be an <see cref="ILocator"/> or <see cref="PageObject"/> property
    /// with exactly one such attribute. Invalid property or attribute combinations throw.
    /// </remarks>
    internal static LocatorMetadataAttribute? GetBindingAttribute(Type objectType, PropertyInfo property)
    {
        var isAutoBoundProperty = SupportedAutoBoundPropertyTypes.Any(t => t.IsAssignableFrom(property.PropertyType));

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

        if (attributes.Length == 0)
        {
            return null;
        }

        if (attributes.Length > 1)
        {
            throw new PageObjectBindingException(
                $"Property '{objectType.Name}.{property.Name}' must have at most one '{nameof(LocatorMetadataAttribute)}' attribute! " +
                $"Found {attributes.Length}.");
        }

        return attributes[0];
    }

    /// <summary>
    /// Resolves a <see cref="LocatorMetadataAttribute"/> to a Playwright <see cref="ILocator"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DataTestIdAttribute"/> uses a test ID.
    /// <see cref="LocatorAttribute"/> supports role, text, CSS, and XPath strategies.
    /// </remarks>
    internal static ILocator ResolveLocator(IPage page, ILocator? baseLocator, LocatorMetadataAttribute attribute)
        => attribute switch
        {
            DataTestIdAttribute testIdAttribute => ResolveDataTestIdAttribute(page, baseLocator, testIdAttribute),
            LocatorAttribute locatorAttribute => ResolveLocatorAttribute(page, baseLocator, locatorAttribute),
            _ => throw new PageObjectBindingException($"Unsupported locator attribute type '{attribute.GetType().Name}'.")
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
            throw new PageObjectBindingException("A Locator selector cannot be empty.");
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

    private static ILocator ResolveByCss(IPage page, ILocator? baseLocator, string selector)
        => ResolveBySelector(page, baseLocator, $"css={selector}");

    private static ILocator ResolveByXPath(IPage page, ILocator? baseLocator, string selector)
        => ResolveBySelector(page, baseLocator, $"xpath={selector}");

    private static ILocator ResolveBySelector(IPage page, ILocator? baseLocator, string selector)
        => baseLocator is null ? page.Locator(selector) : baseLocator.Locator(selector);
}
