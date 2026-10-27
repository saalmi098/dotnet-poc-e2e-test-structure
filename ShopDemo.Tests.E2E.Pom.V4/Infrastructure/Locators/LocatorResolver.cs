using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;

/// <summary>
/// Validates <see cref="LocatorAttribute"/> on component properties and resolves selectors to Playwright locators.
/// </summary>
internal static class LocatorResolver
{
    /// <summary>
    /// Defines the property types that can be auto-bound by <see cref="ComponentTreeBuilder"/>.
    /// </summary>
    private static IReadOnlyList<Type> SupportedAutoBoundPropertyTypes { get; } =
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
        var attributes = property.GetCustomAttributes<LocatorAttribute>(inherit: true).ToArray();
        if (attributes.Length == 0)
        {
            return null;
        }

        if (!SupportedAutoBoundPropertyTypes.Any(type => type.IsAssignableFrom(property.PropertyType)))
        {
            throw new PageObjectBindingException(
                $"Property '{objectType.Name}.{property.Name}' has a locator attribute but is not an supported type. " +
                $"Supported types: {string.Join(", ", SupportedAutoBoundPropertyTypes.Select(t => t.Name))}.");
        }

        if (attributes.Length > 1)
        {
            throw new PageObjectBindingException(
                $"Property '{objectType.Name}.{property.Name}' must have at most one '{nameof(LocatorAttribute)}' attribute. Found {attributes.Length}.");
        }

        return attributes[0];
    }

    /// <summary>
    /// Resolves a <see cref="LocatorAttribute"/> to a Playwright <see cref="ILocator"/>.
    /// </summary>
    /// <remarks>
    /// The selector strategy and strategy-specific options are validated before creating the locator.
    /// </remarks>
    internal static ILocator ResolveLocator(IPage page, ILocator? scope, LocatorAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Selector))
        {
            throw new PageObjectBindingException($"Selector for locator kind '{attribute.Kind}' cannot be empty.");
        }

        if (attribute.Name is not null && attribute.Kind != LocatorKind.Role)
        {
            throw new PageObjectBindingException(
                $"The Name option is only supported for '{LocatorKind.Role}' locators.");
        }

        return attribute.Kind switch
        {
            LocatorKind.Role => ResolveByRole(page, scope, attribute),
            LocatorKind.DataTestId => scope is null
                ? page.GetByTestId(attribute.Selector)
                : scope.GetByTestId(attribute.Selector),
            LocatorKind.Text => scope is null
                ? page.GetByText(attribute.Selector, new() { Exact = true })
                : scope.GetByText(attribute.Selector, new() { Exact = true }),
            LocatorKind.Css => scope is null
                ? page.Locator($"css={attribute.Selector}")
                : scope.Locator($"css={attribute.Selector}"),
            LocatorKind.XPath => scope is null
                ? page.Locator($"xpath={attribute.Selector}")
                : scope.Locator($"xpath={attribute.Selector}"),
            _ => throw new PageObjectBindingException($"Unsupported locator kind '{attribute.Kind}'.")
        };
    }

    private static ILocator ResolveByRole(IPage page, ILocator? scope, LocatorAttribute attribute)
    {
        if (!Enum.TryParse<AriaRole>(attribute.Selector, true, out var role) || !Enum.IsDefined(role))
        {
            throw new PageObjectBindingException($"'{attribute.Selector}' is not a valid Playwright ARIA role.");
        }

        return scope is null
            ? page.GetByRole(role, new() { Name = attribute.Name, Exact = true })
            : scope.GetByRole(role, new() { Name = attribute.Name, Exact = true });
    }
}
