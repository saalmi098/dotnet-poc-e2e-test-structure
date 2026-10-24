using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Otel;

/// <summary>
/// Entry point for tracing a user interaction or test step against a given <see cref="IPage"/>, without the
/// caller needing to know about <see cref="UserInteractionTracer"/> or <see cref="UserInteractionTelemetry"/>.
/// Keeps the OpenTelemetry wiring (looking up the test's root span, building the tracer call) out of the POM
/// classes that use it (<c>Control</c>, <c>PageObjectFactory</c>, ...); they just call
/// <c>PageTracing.TraceInteractionAsync(Page, ...)</c>.
/// </summary>
public static class PageTracing
{
    /// <inheritdoc cref="UserInteractionTracer.TraceAsync(string,string,string?,Func{Task},System.Diagnostics.Activity?,string?,IReadOnlyDictionary{string,object?}?)"/>
    public static Task TraceInteractionAsync(
        IPage page,
        string componentType,
        string interactionName,
        Func<Task> operation,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
        => UserInteractionTracer.TraceAsync(
            componentType,
            interactionName,
            page.Url,
            operation,
            UserInteractionTelemetry.GetTestRootActivity(page),
            target,
            attributes);

    /// <inheritdoc cref="UserInteractionTracer.TraceAsync{TResult}"/>
    public static Task<TResult> TraceInteractionAsync<TResult>(
        IPage page,
        string componentType,
        string interactionName,
        Func<Task<TResult>> operation,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
        => UserInteractionTracer.TraceAsync(
            componentType,
            interactionName,
            page.Url,
            operation,
            UserInteractionTelemetry.GetTestRootActivity(page),
            target,
            attributes);
}
