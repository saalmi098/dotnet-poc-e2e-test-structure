using System.Diagnostics;
using OpenTelemetry.Trace;

namespace ShopDemo.Tests.E2E.Otel;

/// <summary>
/// Wraps a single simulated user interaction (a click, a selection, typing into a field, ...) in an OpenTelemetry
/// span so that frontend interactions driven by the E2E tests show up as traces, same as a real user-monitoring
/// (RUM) integration would record them in the browser.
/// </summary>
internal static class UserInteractionTracer
{
    /// <summary>
    /// Semantic-ish attribute names used on interaction spans. Not an official OTel semantic convention;
    /// namespaced under <c>ui.*</c> to avoid colliding with one if adopted later.
    /// </summary>
    public static class Attributes
    {
        public const string ComponentType = "ui.component.type";
        public const string Interaction = "ui.interaction";
        public const string PageUrl = "ui.page.url";
        public const string Target = "ui.component.target";
    }

    /// <summary>
    /// Starts a span named <c>"{componentType}.{interactionName}"</c> (or <c>"{componentType}.{interactionName}:
    /// {target}"</c> when <paramref name="target"/> is given), runs <paramref name="operation"/> inside it, and
    /// records success/failure on the span. The caller remains responsible for raising the original exception.
    /// </summary>
    /// <param name="componentType">The control/page-object type performing the interaction, e.g. "Button".</param>
    /// <param name="interactionName">The interaction being performed, e.g. "Click", "SelectOption", "Fill".</param>
    /// <param name="pageUrl">The current page URL, recorded as a span attribute when available.</param>
    /// <param name="operation">The Playwright interaction to execute inside the span.</param>
    /// <param name="parent">
    /// The test's root span (see <see cref="UserInteractionTelemetry.GetTestRootActivity"/>), so this
    /// interaction nests under it instead of starting its own, disconnected trace. Null starts a new trace.
    /// </param>
    /// <param name="target">
    /// A human-readable identifier for what the interaction acted on (a button's label, the tab clicked, the
    /// option selected, ...), folded into the span name so distinct interactions of the same control type are
    /// distinguishable at a glance instead of all showing up as e.g. "Button.Click". Not the raw locator/selector
    /// — those are often dynamic and not meaningful to read. Omit when no good label is available.
    /// </param>
    /// <param name="attributes">Additional attributes to attach to the span (e.g. the option text selected).</param>
    public static Task TraceAsync(
        string componentType,
        string interactionName,
        string? pageUrl,
        Func<Task> operation,
        Activity? parent,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
        => TraceAsync<object?>(
            componentType,
            interactionName,
            pageUrl,
            async () => { await operation(); return null; },
            parent,
            target,
            attributes);

    /// <summary>
    /// Same as <see cref="TraceAsync(string,string,string?,Func{Task},Activity?,string?,IReadOnlyDictionary{string,object?}?)"/>,
    /// for an <paramref name="operation"/> that produces a result (e.g. binding a page object).
    /// </summary>
    public static async Task<TResult> TraceAsync<TResult>(
        string componentType,
        string interactionName,
        string? pageUrl,
        Func<Task<TResult>> operation,
        Activity? parent,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
    {
        var name = string.IsNullOrEmpty(target)
            ? $"{componentType}.{interactionName}"
            : $"{componentType}.{interactionName}: {target}";

        using var activity = parent is not null
            ? UserInteractionTelemetry.ActivitySource.StartActivity(name, ActivityKind.Internal, parent.Context)
            : UserInteractionTelemetry.ActivitySource.StartActivity(name, ActivityKind.Internal);

        activity?.SetTag(Attributes.ComponentType, componentType);
        activity?.SetTag(Attributes.Interaction, interactionName);
        if (pageUrl is not null)
        {
            activity?.SetTag(Attributes.PageUrl, pageUrl);
        }

        if (!string.IsNullOrEmpty(target))
        {
            activity?.SetTag(Attributes.Target, target);
        }

        if (attributes is not null)
        {
            foreach (var (key, value) in attributes)
            {
                activity?.SetTag(key, value);
            }
        }

        try
        {
            var result = await operation();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);
            throw;
        }
    }
}
