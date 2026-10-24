using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Otel;
using System.Text.RegularExpressions;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// Owns the OpenTelemetry instrumentation for one <see cref="Control"/> instance. Composed into
/// <see cref="Control"/> (same pattern as <c>ComponentBindingContext</c>) rather than implemented directly on
/// it, so tracing stays a self-contained concern instead of growing inside the class that owns binding and
/// interaction behaviour.
/// </summary>
/// <param name="page">The Playwright page the owning control interacts with.</param>
/// <param name="componentType">The owning control's type name, e.g. "Button".</param>
/// <param name="getLocator">Resolves the owning control's root locator, lazily (called per interaction).</param>
/// <param name="getLabel">Resolves the owning control's own human-readable label, lazily; see <see cref="Control.GetLabel"/>.</param>
internal sealed partial class ControlTracing(IPage page, string componentType, Func<ILocator> getLocator, Func<Task<string?>> getLabel)
{
    // Matches each `data-testid="..."` segment Playwright's own locator syntax embeds, e.g.
    // `Locator@internal:testid=[data-testid="inventory-item-canvas-tote"] >> internal:testid=[data-testid="inventory-stock-canvas-tote"]`.
    private static readonly Regex TestIdPattern = DataTestIdRegex();

    /// <summary>
    /// Runs a user interaction (click, selection, typing, ...) inside an OpenTelemetry span named
    /// <c>"{ControlType}.{interactionName}"</c>, so the interaction is reported the same way a real
    /// frontend RUM integration would report it.
    /// </summary>
    /// <param name="interactionName">The interaction being performed, e.g. "Click", "SelectOption".</param>
    /// <param name="operation">The Playwright interaction to execute inside the span.</param>
    /// <param name="target">
    /// A human-readable identifier for what was interacted with (button label, tab text, option selected, ...),
    /// folded into the span name/tags so e.g. two different buttons don't both just show up as "Button.Click".
    /// Defaults to the owning control's own label when not given explicitly.
    /// </param>
    /// <param name="attributes">Additional span attributes, e.g. the option text selected.</param>
    public async Task TraceInteraction(
        string interactionName,
        Func<Task> operation,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
    {
        target ??= await TryGetLabel();

        // EXPERIMENTAL: comparing label-based naming against the locator in Jaeger; see the README's
        // "UI interaction tracing" section.
        var locatorDescription = TryDescribeLocator();

        await PageTracing.TraceInteractionAsync(page, componentType, interactionName, operation, target, attributes, locatorDescription);
    }

    // Reading the label is a nice-to-have for the span name; a transient failure must not break the interaction.
    private async Task<string?> TryGetLabel()
    {
        try
        {
            return await getLabel();
        }
        catch
        {
            return null;
        }
    }

    // Describing the locator is a nice-to-have for the span name; a transient failure must not break the interaction.
    // Condensed to just the test-id path (e.g. "inventory-item-canvas-tote/inventory-stock-canvas-tote") instead of
    // Locator.ToString()'s full Playwright engine syntax: same uniqueness, far shorter, nothing app-specific lost
    // since every control is matched by data-testid anyway. Falls back to the raw description for the rare locator
    // that isn't (e.g. a CSS-only one), so nothing is silently dropped.
    private string? TryDescribeLocator()
    {
        try
        {
            var raw = getLocator().ToString() ?? string.Empty;
            var testIds = TestIdPattern.Matches(raw).Select(match => match.Groups[1].Value).ToArray();
            return testIds.Length > 0 ? string.Join('/', testIds) : raw;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex("data-testid=\"([^\"]+)\"", RegexOptions.Compiled)]
    private static partial Regex DataTestIdRegex();
}
