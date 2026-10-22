using Microsoft.Playwright;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Constructs bindable components through their public constructor convention.
/// </summary>
internal static class ComponentActivator
{
    /// <summary>
    /// Validates a component type and constructor, then creates an unbound instance.
    /// </summary>
    public static IBindableComponent Create(Type componentType, IPage page, ILocator? baseLocator)
    {
        if (!typeof(IBindableComponent).IsAssignableFrom(componentType))
        {
            throw new PageObjectBindingException(
                $"Type '{componentType.Name}' must implement '{nameof(IBindableComponent)}'.");
        }

        var constructors = componentType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (componentType.IsAbstract || constructors.Length != 1 || !IsSupportedConstructor(constructors[0]))
        {
            throw new PageObjectBindingException(
                $"Bindable type '{componentType.Name}' must expose exactly one public constructor with signature '(IPage page, ILocator? baseLocator = null)'. " +
                $"Found {constructors.Length} public constructor(s).");
        }

        try
        {
            return (IBindableComponent)constructors[0].Invoke([page, baseLocator]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new PageObjectBindingException(
                $"Could not construct type '{componentType.Name}' using signature '(IPage page, ILocator? baseLocator = null)'.",
                exception.InnerException);
        }
    }

    private static bool IsSupportedConstructor(ConstructorInfo constructor)
    {
        var parameters = constructor.GetParameters();

        return parameters.Length == 2
            && parameters[0].ParameterType == typeof(IPage)
            && parameters[1].ParameterType == typeof(ILocator)
            && parameters[1].IsOptional
            && parameters[1].DefaultValue is null;
    }
}
