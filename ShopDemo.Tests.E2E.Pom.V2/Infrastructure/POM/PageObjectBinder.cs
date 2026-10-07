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
