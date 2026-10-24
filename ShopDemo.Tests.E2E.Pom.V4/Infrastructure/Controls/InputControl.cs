using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// Base class for controls wrapping a single <c>&lt;input&gt;</c> element. The locator attribute may match the input
/// itself or a wrapper around it.
/// </summary>
public abstract class InputControl(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
{
    // Depending on the MudBlazor component, the matched element is either the input itself (MudTextField forwards
    // data-testid to it) or a wrapper around it (the V2 inventory page reaches its MudNumericField through a nested
    // input). descendant-or-self finds the input in both cases.
    protected ILocator Input => Locator.Locator("xpath=descendant-or-self::input");

    protected Task FillText(string value) => TraceInteraction(
        "Fill",
        async () =>
        {
            Trace($"Filling input with '{value}'.");
            await Input.FillAsync(value);
        },
        target: value);

    public Task<string> GetValue() => Input.InputValueAsync();
}
