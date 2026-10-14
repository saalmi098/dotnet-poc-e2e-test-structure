using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A single-line text input (<c>MudTextField</c>).
/// </summary>
public sealed class TextField(IPage page, ILocator? baseLocator = null) : InputControl(page, baseLocator)
{
    public Task Fill(string value) => FillText(value);
}
