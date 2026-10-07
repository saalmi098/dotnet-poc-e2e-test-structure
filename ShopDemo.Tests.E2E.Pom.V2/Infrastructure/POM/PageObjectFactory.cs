using Microsoft.Playwright;
using System.Reflection;

namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

/// <summary>
/// Creates <see cref="PageObject"/> instances using reflection, binds their annotated properties, and waits for required locators.
/// </summary>
/// <remarks>
/// Page-object types must expose exactly one public constructor with signature
/// <c>(IPage page, ILocator? baseLocator = null)</c>.
/// </remarks>
public sealed class PageObjectFactory
{
    /// <summary>
    /// Creates a <see cref="PageObject"/>, binds its annotated properties, and waits for required locators.
    /// </summary>
    public async Task<T> Create<T>(IPage page, ILocator? baseLocator = null) where T : PageObject
        => (T)await Create(typeof(T), page, baseLocator, waitForRequiredLocators: true);

    /// <summary>
    /// Constructs and binds a <see cref="PageObject"/> within the optional locator scope.
    /// </summary>
    /// <remarks>
    /// Optional nested objects still receive locators, but can skip initial waits for their
    /// required locators.
    /// </remarks>
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
