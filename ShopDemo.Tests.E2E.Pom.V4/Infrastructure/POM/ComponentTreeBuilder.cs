using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Builds a component tree by creating components, binding attributed properties, and waiting for required UI elements.
/// </summary>
/// <remarks>
/// One instance is created per root page object. Its active recursion path is shared across that traversal,
/// while locator scope and wait behavior are passed per child.
/// </remarks>
internal sealed class ComponentTreeBuilder(IPage page)
{
    private readonly HashSet<Type> _activeComponentTypes = [];
    private readonly IPage _page = page;

    /// <summary>
    /// Creates a component, binds its attributed properties recursively, and registers its readiness requirements.
    /// </summary>
    public async Task<IBindableComponent> Build(
        Type componentType,
        ILocator? baseLocator,
        bool waitForRequiredLocators)
    {
        if (!_activeComponentTypes.Add(componentType))
        {
            throw new PageObjectBindingException(
                $"Recursive component binding detected for '{componentType.FullName}'. Remove the cyclic component property.");
        }

        try
        {
            var instance = ComponentActivator.Create(componentType, _page, baseLocator);
            await BindProperties(instance, baseLocator, waitForRequiredLocators);
            return instance;
        }
        finally
        {
            _activeComponentTypes.Remove(componentType);
        }
    }

    private static void ValidateProperty(IBindableComponent instance, PropertyInfo property, LocatorAttribute attribute)
    {
        var componentType = instance.GetType();
        if (property.SetMethod is null || property.GetIndexParameters().Length != 0)
        {
            throw new PageObjectBindingException(
                $"Property '{componentType.Name}.{property.Name}' must have a setter and must not be an indexer.");
        }

        if (!typeof(ILocator).IsAssignableFrom(property.PropertyType)
            && !typeof(IBindableComponent).IsAssignableFrom(property.PropertyType))
        {
            throw new PageObjectBindingException(
                $"Property '{componentType.Name}.{property.Name}' must implement '{nameof(IBindableComponent)}' or be assignable to '{nameof(ILocator)}'.");
        }

        if (!attribute.Optional
            && typeof(IBindableComponent).IsAssignableFrom(property.PropertyType)
            && instance is not PageObject)
        {
            throw new PageObjectBindingException(
                $"Required component property '{componentType.Name}.{property.Name}' can only be bound on a page object. " +
                "Controls are leaf components and cannot own required child components.");
        }
    }

    private static async Task WaitForRequiredLocator(ILocator locator, PropertyInfo property, LocatorAttribute attribute)
    {
        try
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            throw new PageObjectBindingException(
                $"Required locator for property '{property.DeclaringType?.Name}.{property.Name}' was not visible. " +
                $"Selector: {attribute.Describe()}.",
                exception);
        }
    }

    private async Task BindChildComponent(
        IBindableComponent instance,
        PropertyInfo property,
        LocatorAttribute attribute,
        ILocator locator,
        bool wait)
    {
        var child = await Build(property.PropertyType, locator, wait);
        property.SetValue(instance, child);

        if (child is Control control)
        {
            if (wait)
            {
                await WaitForRequiredLocator(control.Locator, property, attribute);
            }

            control.AddRequiredLocator(control.Locator);
        }

        if (!attribute.Optional && instance is PageObject pageObject)
        {
            pageObject.AddRequiredChild(child);
        }
    }

    private async Task BindProperties(
        IBindableComponent instance,
        ILocator? baseLocator,
        bool waitForRequiredLocators)
    {
        var componentType = instance.GetType();
        foreach (var property in componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attribute = LocatorResolver.GetBindingAttribute(componentType, property);
            if (attribute is not null)
            {
                await BindProperty(instance, property, attribute, baseLocator, waitForRequiredLocators);
            }
        }
    }

    private async Task BindProperty(
        IBindableComponent instance,
        PropertyInfo property,
        LocatorAttribute attribute,
        ILocator? baseLocator,
        bool waitForRequiredLocators)
    {
        ValidateProperty(instance, property, attribute);
        var locator = LocatorResolver.ResolveLocator(_page, baseLocator, attribute);

        // Optional ancestors make their entire subtree non-blocking during initial binding.
        var wait = !attribute.Optional && waitForRequiredLocators;
        var isControl = typeof(Control).IsAssignableFrom(property.PropertyType);
        if (wait && !isControl)
        {
            await WaitForRequiredLocator(locator, property, attribute);
        }

        if (typeof(ILocator).IsAssignableFrom(property.PropertyType))
        {
            property.SetValue(instance, locator);
            if (!attribute.Optional)
            {
                instance.AddRequiredLocator(locator);
            }
        }
        else
        {
            await BindChildComponent(instance, property, attribute, locator, wait);
        }
    }
}