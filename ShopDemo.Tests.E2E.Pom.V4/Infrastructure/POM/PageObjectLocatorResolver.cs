using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Validates <see cref="LocatorAttribute"/> on component properties and resolves selectors to Playwright locators.
/// </summary>
internal static class PageObjectLocatorResolver
{
    /// <summary>
    /// Defines the property types that can be auto-bound by <see cref="PageObjectBinder"/>.
    /// </summary>
    private static Type[] SupportedAutoBoundPropertyTypes =>
    [
        typeof(ILocator),
        typeof(IBindableComponent) // for nested page-object and control properties
    ];

    /// <summary>
    /// Returns the binding attribute for an auto-bound property, or null for a property without one.
    /// </summary>
    /// <remarks>
    /// Only properties carrying a locator metadata attribute are bound; all others are ignored and keep their
    /// default value. A bound property must be an <see cref="ILocator"/> or <see cref="IBindableComponent"/> property
    /// with exactly one such attribute. Invalid property or attribute combinations throw.
    /// </remarks>
    internal static LocatorAttribute? GetBindingAttribute(Type objectType, PropertyInfo property)
    {
        var isAutoBoundProperty = SupportedAutoBoundPropertyTypes.Any(t => t.IsAssignableFrom(property.PropertyType));

        var attributes = property
            .GetCustomAttributes<LocatorAttribute>(inherit: true)
            .ToArray();

        if (!isAutoBoundProperty)
        {
            if (attributes.Length > 0)
            {
                throw new PageObjectBindingException(
                    $"Property '{objectType.Name}.{property.Name}' has a locator attribute but is not an ILocator, PageObject or Control property.");
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
                $"Property '{objectType.Name}.{property.Name}' must have at most one '{nameof(LocatorAttribute)}' attribute! " +
                $"Found {attributes.Length}.");
        }

        return attributes[0];
    }

    /// <summary>
    /// Resolves a <see cref="LocatorAttribute"/> to a Playwright <see cref="ILocator"/>.
    /// </summary>
    /// <remarks>
    /// The selector strategy and strategy-specific options are validated before creating the locator.
    /// </remarks>
    internal static ILocator ResolveLocator(IPage page, ILocator? baseLocator, LocatorAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Selector))
        {
            throw new PageObjectBindingException($"Selector for locator kind '{attribute.Kind}' cannot be empty.");
        }

        if (attribute.Name is not null && attribute.Kind != LocatorKind.Role)
        {
            throw new PageObjectBindingException(
                $"The Name option is only supported for '{LocatorKind.Role}' locators, not '{attribute.Kind}'.");
        }

        return attribute.Kind switch
        {
            LocatorKind.DataTestId => ResolveByTestId(page, baseLocator, attribute.Selector),
            LocatorKind.Role => ResolveByRole(page, baseLocator, attribute),
            LocatorKind.Text => ResolveByText(page, baseLocator, attribute),
            LocatorKind.Css => ResolveByCss(page, baseLocator, attribute.Selector),
            LocatorKind.XPath => ResolveByXPath(page, baseLocator, attribute.Selector),
            _ => throw new PageObjectBindingException(
                $"Unsupported locator kind '{attribute.Kind}' on selector '{attribute.Selector}'.")
        };
    }

    private static ILocator ResolveByTestId(IPage page, ILocator? baseLocator, string testId)
        => baseLocator is null ? page.GetByTestId(testId) : baseLocator.GetByTestId(testId);

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
                Exact = true
            });
        }

        return page.GetByRole(role, new PageGetByRoleOptions
        {
            Name = attribute.Name,
            Exact = true
        });
    }

    private static ILocator ResolveByText(IPage page, ILocator? baseLocator, LocatorAttribute attribute)
    {
        if (baseLocator is not null)
        {
            return baseLocator.GetByText(attribute.Selector, new LocatorGetByTextOptions
            {
                Exact = true
            });
        }

        return page.GetByText(attribute.Selector, new PageGetByTextOptions
        {
            Exact = true
        });
    }

    private static ILocator ResolveByCss(IPage page, ILocator? baseLocator, string selector)
        => ResolveBySelector(page, baseLocator, $"css={selector}");

    private static ILocator ResolveByXPath(IPage page, ILocator? baseLocator, string selector)
        => ResolveBySelector(page, baseLocator, $"xpath={selector}");

    private static ILocator ResolveBySelector(IPage page, ILocator? baseLocator, string selector)
        => baseLocator is null ? page.Locator(selector) : baseLocator.Locator(selector);
}
