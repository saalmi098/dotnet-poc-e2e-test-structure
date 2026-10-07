using Microsoft.Playwright;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

public sealed class PageObjectFactory
{
    public async Task<T> Create<T>(IPage page, ILocator? baseLocator = null) where T : PageObject
        => (T)await Create(typeof(T), page, baseLocator, waitForRequiredLocators: true);

    internal async Task<PageObject> Create(
        Type objectType,
        IPage page,
        ILocator? baseLocator,
        bool waitForRequiredLocators)
    {
        var constructors = objectType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .ToArray();

        if (constructors.Length != 1 || !IsSupportedConstructor(constructors[0]))
        {
            throw new PageObjectBindingException(
                $"Type '{objectType.Name}' must expose exactly one public constructor with signature '(IPage page, ILocator? baseLocator = null)'! " +
                $"Found {constructors.Length} public constructor(s).");
        }

        PageObject instance;
        try
        {
            instance = (PageObject)constructors[0].Invoke([page, baseLocator]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new PageObjectBindingException(
                $"Could not construct type '{objectType.Name}' using signature '(IPage page, ILocator? baseLocator = null)'.",
                exception.InnerException);
        }

        await PageObjectBinder.Bind(instance, page, baseLocator, this, waitForRequiredLocators);
        return instance;
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
