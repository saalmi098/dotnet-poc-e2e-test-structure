using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A clickable button (<c>MudButton</c>).
/// </summary>
public sealed class Button(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    public async Task Click()
    {
        Trace("Clicking button.");
        await Locator.ClickAsync();
    }

    public Task<string> GetText() => Locator.InnerTextAsync();
}
