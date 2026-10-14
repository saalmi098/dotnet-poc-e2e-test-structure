using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Shared;
using System.Diagnostics;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Binds locator metadata to the <see cref="ILocator"/> and nested <see cref="Component"/> properties
/// of a constructed <see cref="Component"/>.
/// </summary>
internal static class PageObjectBinder
{
    /// <summary>
    /// Processes public properties, resolves their locator metadata, and waits for required locators when requested.
    /// Nested components are bound recursively with their matched parent locator as the scope.
    /// </summary>
    internal static async Task Bind(
        Component instance,
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
            var wait = attribute.Required && waitForRequiredLocators;

            // For most properties the element matched by the attribute is exactly what must become visible, so it is
            // waited for right here. A control is different: the matched element is only a starting point and the
            // control decides which element is its real root (e.g. MudSelect puts the test id on a hidden input, so
            // the visible root is an ancestor). That root is only known once the control is constructed, so for
            // controls the wait happens further below, on the resolved root.
            var isControl = typeof(Control).IsAssignableFrom(property.PropertyType);

            if (wait && !isControl)
            {
                await WaitForRequiredLocator(locator, objectType, property, attribute);
            }

            if (typeof(ILocator).IsAssignableFrom(property.PropertyType))
            {
                // bind locator property
                property.SetValue(instance, locator);

                if (attribute.Required)
                {
                    instance.AddRequiredLocator(locator);
                }
            }
            else if (typeof(Component).IsAssignableFrom(property.PropertyType))
            {
                // create and bind child component property
                var child = await factory.Create(property.PropertyType, page, locator, wait);
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

                if (attribute.Required)
                {
                    instance.AddRequiredChild(child);
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
        LocatorMetadataAttribute attribute)
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
        LocatorMetadataAttribute attribute)
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
