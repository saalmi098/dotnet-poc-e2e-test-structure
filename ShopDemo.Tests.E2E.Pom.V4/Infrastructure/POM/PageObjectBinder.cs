using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Shared;
using System.Diagnostics;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Binds locator metadata to the <see cref="ILocator"/> and nested <see cref="IBindableComponent"/> properties
/// of a constructed <see cref="IBindableComponent"/>.
/// </summary>
internal static class PageObjectBinder
{
    /// <summary>
    /// Processes public properties, resolves their locator metadata, and waits for required locators when requested.
    /// Nested components are bound recursively with their matched parent locator as the scope.
    /// </summary>
    internal static async Task Bind(
        IBindableComponent instance,
        IPage page,
        ILocator? baseLocator,
        PageObjectFactory factory,
        bool waitForRequiredLocators)
    {
        var objectType = instance.GetType();
        var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            var attribute = PageObjectLocatorResolver.GetBindingAttribute(objectType, property);
            if (attribute is null)
            {
                continue;
            }

            if (property.SetMethod is null)
            {
                throw new PageObjectBindingException($"Property '{objectType.Name}.{property.Name}' must have a setter to be auto-bound.");
            }

            var locator = PageObjectLocatorResolver.ResolveLocator(page, baseLocator, attribute);
            var wait = !attribute.Optional && waitForRequiredLocators;

            // Usually locator attributes match the visible element, so the binder can wait immediately.
            // But: Some controls may map an internal match to a different visible root; for example, MudSelect puts
            // its test ID on a hidden input (therefore cannot be waited on directly). Create the control first, then wait for its visible root below.
            var isControl = typeof(Control).IsAssignableFrom(property.PropertyType);

            if (wait && !isControl)
            {
                await WaitForRequiredLocator(locator, objectType, property, attribute);
            }

            if (typeof(ILocator).IsAssignableFrom(property.PropertyType))
            {
                // bind locator property
                property.SetValue(instance, locator);

                if (!attribute.Optional)
                {
                    instance.AddRequiredLocator(locator);
                }
            }
            else if (typeof(IBindableComponent).IsAssignableFrom(property.PropertyType))
            {
                // create and bind child component property
                var child = await factory.CreateAndBindComponent(property.PropertyType, page, locator, wait);
                property.SetValue(instance, child);

                if (child is Control control)
                {
                    // The control now exists, so its resolved root (control.Locator) is known: wait for that root
                    // (the skipped wait above), and register it so the control's IsReady() checks the same element.
                    if (wait)
                    {
                        await WaitForRequiredLocator(control.Locator, objectType, property, attribute);
                    }

                    control.AddRequiredLocator(control.Locator);
                }

                if (!attribute.Optional)
                {
                    if (instance is not PageObject pageObject)
                    {
                        throw new PageObjectBindingException(
                            $"Required component property '{objectType.Name}.{property.Name}' can only be bound on a page object. " +
                            "Controls are leaf components and cannot own required child components.");
                    }

                    pageObject.AddRequiredChild(child);
                }
            }
            else
            {
                throw new PageObjectBindingException($"Property '{objectType.Name}.{property.Name}' has an unsupported type " +
                    $"'{property.PropertyType.FullName}' for auto-binding.");
            }
        }
    }

    private static async Task WaitForRequiredLocator(
        ILocator locator,
        Type objectType,
        PropertyInfo property,
        LocatorAttribute attribute)
    {
        var waitDuration = Stopwatch.StartNew();
        E2ETrace.Write(
            typeof(PageObjectBinder),
            $"Waiting for required locator '{objectType.Name}.{property.Name}' ({attribute.Describe()}).");

        await WaitUntilVisible(locator, objectType, property, attribute);

        E2ETrace.Write(
            typeof(PageObjectBinder),
            $"Required locator '{objectType.Name}.{property.Name}' is visible after {waitDuration.Elapsed}.");
    }

    private static async Task WaitUntilVisible(
        ILocator locator,
        Type objectType,
        PropertyInfo property,
        LocatorAttribute attribute)
    {
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
}
