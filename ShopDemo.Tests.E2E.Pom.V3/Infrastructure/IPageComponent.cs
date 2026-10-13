using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

/// <summary>
/// Implemented by every top-level page/tab object so <see cref="PageComponent.Create{T}"/> can
/// construct and wait for it uniformly. A CRTP-style static-abstract interface member stands in
/// for V2's reflective constructor convention, fully compile-time checked, no reflection.
/// </summary>
public interface IPageComponent<TSelf> where TSelf : PageComponent
{
    static abstract TSelf CreateInstance(IPage page, ILocator? baseLocator);
}
