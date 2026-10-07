using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

/// <summary>
/// Binds locator metadata to the <see cref="ILocator"/> and nested <see cref="PageObject"/> properties
/// of a constructed <see cref="PageObject"/>.
/// </summary>
internal static class PageObjectBinder
{
    /// <summary>
    /// Processes public properties, resolves their locator metadata, and waits for required locators when requested.
    /// Nested page objects are bound recursively with their matched parent locator as the scope.
    /// </summary>
    internal static async Task Bind(
        PageObject instance,
        IPage page,
        ILocator? baseLocator,
        PageObjectFactory factory,
        bool waitForRequiredLocators)
    {
        var objectType = instance.GetType();
        var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            await BindProperty(instance, objectType, property, page, baseLocator, factory, waitForRequiredLocators);
        }
    }

    private static async Task BindProperty(
        PageObject instance,
        Type objectType,
        PropertyInfo property,
        IPage page,
        ILocator? baseLocator,
        PageObjectFactory factory,
        bool waitForRequiredLocators)
    {
        var attribute = PageObjectLocatorResolver.GetBindingAttribute(objectType, property);
        if (attribute is null)
        {
            return;
        }

        ValidateProperty(objectType, property);
        var locator = ResolveForProperty(page, baseLocator, objectType, property, attribute);
        await WaitForRequiredLocator(locator, objectType, property, attribute, waitForRequiredLocators);

        if (typeof(ILocator).IsAssignableFrom(property.PropertyType))
        {
            BindLocatorProperty(instance, property, locator, attribute);
            return;
        }

        await BindNestedPageObject(
            instance,
            property,
            locator,
            attribute,
            page,
            factory,
            waitForRequiredLocators);
    }

    private static void ValidateProperty(Type objectType, PropertyInfo property)
    {
        if (property.GetMethod is null || property.SetMethod is null)
        {
            throw new PageObjectBindingException(
                $"Property '{objectType.Name}.{property.Name}' must have a getter and setter to be auto-bound.");
        }
    }

    /// <summary>
    /// Creates the Playwright <see cref="ILocator"/> declared for a property in a <see cref="PageObject"/>.
    /// Throws if the locator cannot be resolved.
    /// </summary>
    private static ILocator ResolveForProperty(
        IPage page,
        ILocator? baseLocator,
        Type objectType,
        PropertyInfo property,
        LocatorMetadataAttribute attribute)
    {
        try
        {
            return PageObjectLocatorResolver.Resolve(page, baseLocator, attribute);
        }
        catch (Exception exception) when (exception is PlaywrightException or ArgumentException or InvalidOperationException)
        {
            throw new PageObjectBindingException(
                $"Could not resolve property '{objectType.Name}.{property.Name}' using selector {attribute.Describe()}.",
                exception);
        }
    }

    private static async Task WaitForRequiredLocator(
        ILocator locator,
        Type objectType,
        PropertyInfo property,
        LocatorMetadataAttribute attribute,
        bool waitForRequiredLocators)
    {
        if (!attribute.Required || !waitForRequiredLocators)
        {
            return;
        }

        try
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            throw new PageObjectBindingException(
                $"Required locator for property '{objectType.Name}.{property.Name}' was not visible. " +
                $"Selector: {attribute.Describe()}.",
                exception);
        }
    }

    private static void BindLocatorProperty(
        PageObject instance,
        PropertyInfo property,
        ILocator locator,
        LocatorMetadataAttribute attribute)
    {
        property.SetValue(instance, locator);
        if (attribute.Required)
        {
            instance.AddRequiredLocator(locator);
        }
    }

    /// <summary>
    /// Creates the child within the matched locator scope and registers it for readiness when required.
    /// </summary>
    private static async Task BindNestedPageObject(
        PageObject instance,
        PropertyInfo property,
        ILocator locator,
        LocatorMetadataAttribute attribute,
        IPage page,
        PageObjectFactory factory,
        bool waitForRequiredLocators)
    {
        var child = await factory.Create(
            property.PropertyType,
            page,
            locator,
            waitForRequiredLocators: attribute.Required && waitForRequiredLocators);

        property.SetValue(instance, child);
        if (attribute.Required)
        {
            instance.AddRequiredChild(child);
        }
    }

}
