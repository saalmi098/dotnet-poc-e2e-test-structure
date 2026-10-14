using Microsoft.Playwright;
using System.Globalization;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// A numeric input (<c>MudNumericField</c>).
/// </summary>
public sealed class NumericField(IPage page, ILocator? baseLocator = null) : InputControl(page, baseLocator)
{
    public Task Fill(int value) => FillText(value.ToString(CultureInfo.InvariantCulture));

    public async Task<int> GetIntValue() => int.Parse(await GetValue(), CultureInfo.InvariantCulture);
}
