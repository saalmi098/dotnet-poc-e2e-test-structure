using System.Diagnostics;
using OpenTelemetry.Trace;

namespace ShopDemo.Tests.E2E.Otel;

/// <summary>
/// Wraps a single simulated user interaction (a click, a selection, typing into a field, ...) in an OpenTelemetry
/// span so that frontend interactions driven by the E2E tests show up as traces, same as a real user-monitoring
/// (RUM) integration would record them in the browser.
/// </summary>
public static class UserInteractionTracer
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
    }

    /// <summary>
    /// Starts a span named <c>"{componentType}.{interactionName}"</c>, runs <paramref name="operation"/> inside it,
    /// and records success/failure on the span. The caller remains responsible for raising the original exception.
    /// </summary>
    /// <param name="componentType">The control/page-object type performing the interaction, e.g. "Button".</param>
    /// <param name="interactionName">The interaction being performed, e.g. "Click", "SelectOption", "Fill".</param>
    /// <param name="pageUrl">The current page URL, recorded as a span attribute when available.</param>
    /// <param name="operation">The Playwright interaction to execute inside the span.</param>
    /// <param name="parent">
    /// The test's root span (see <see cref="UserInteractionTelemetry.GetTestRootActivity"/>), so this
    /// interaction nests under it instead of starting its own, disconnected trace. Null starts a new trace.
    /// </param>
    /// <param name="attributes">Additional attributes to attach to the span (e.g. the option text selected).</param>
    public static async Task TraceAsync(
        string componentType,
        string interactionName,
        string? pageUrl,
        Func<Task> operation,
        Activity? parent,
        IReadOnlyDictionary<string, object?>? attributes = null)
    {
        using var activity = parent is not null
            ? UserInteractionTelemetry.ActivitySource.StartActivity(
                $"{componentType}.{interactionName}",
                ActivityKind.Client,
                parent.Context)
            : UserInteractionTelemetry.ActivitySource.StartActivity(
                $"{componentType}.{interactionName}",
                ActivityKind.Client);

        activity?.SetTag(Attributes.ComponentType, componentType);
        activity?.SetTag(Attributes.Interaction, interactionName);
        if (pageUrl is not null)
        {
            activity?.SetTag(Attributes.PageUrl, pageUrl);
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
            await operation();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);
            throw;
        }
    }
}
