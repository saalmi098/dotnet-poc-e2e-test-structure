using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

/// <summary>
/// Binding contract used to register an object's required locators and check its readiness.
/// </summary>
public interface IBindableComponent
{
    /// <summary>
    /// Registers a locator that contributes to this object's readiness.
    /// </summary>
    void AddRequiredLocator(ILocator locator);

    /// <summary>
    /// Reports whether this object's required locators are currently visible so that the object can be considered ready for interaction.
    /// </summary>
    Task<bool> IsReady();
}
