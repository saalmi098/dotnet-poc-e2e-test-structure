using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ShopDemo.Tests.E2E.Otel;

/// <summary>
/// Owns the OpenTelemetry tracer provider and <see cref="ActivitySource"/> used to record frontend user
/// interactions (clicks, selections, input) performed by the E2E test suite against the Blazor app under test.
/// </summary>
/// <remarks>
/// This is a proof of concept: it exports spans to the console by default, and additionally to an OTLP
/// collector when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set, so the same traces can be viewed in a real
/// backend (e.g. Jaeger, Tempo, Aspire dashboard) without code changes.
/// </remarks>
public static class UserInteractionTelemetry
{
    /// <summary>
    /// Name of the <see cref="ActivitySource"/> that emits user-interaction spans. Must match the source
    /// name registered with <see cref="OpenTelemetry.Trace.TracerProviderBuilderExtensions.AddSource"/>.
    /// </summary>
    public const string SourceName = "ShopDemo.E2E.UserInteractions";

    /// <summary>
    /// The activity source used to start spans for user interactions. Shared by <see cref="UserInteractionTracer"/>.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(SourceName, "1.0.0");

    private static readonly TracerProvider Provider = BuildTracerProvider();

    /// <summary>
    /// Maps each test's <see cref="IPage"/> (one per test, stable for its lifetime) to that test's root span,
    /// so interaction spans can be parented explicitly.
    /// </summary>
    /// <remarks>
    /// Not ambient <see cref="Activity.Current"/>: xunit's in-process runner doesn't flow
    /// <see cref="System.Threading.ExecutionContext"/> from a test's <c>InitializeAsync</c> into the test
    /// body, so that silently produced one disconnected trace per interaction instead of one per test.
    /// </remarks>
    private static readonly ConditionalWeakTable<IPage, Activity> TestRootActivities = new();

    /// <summary>
    /// Registers the root span for the test owning <paramref name="page"/>. Subsequent interaction spans
    /// recorded against that page (see <see cref="UserInteractionTracer"/>) are parented to it.
    /// </summary>
    public static void SetTestRootActivity(IPage page, Activity? activity)
    {
        TestRootActivities.Remove(page);
        if (activity is not null)
        {
            TestRootActivities.Add(page, activity);
        }
    }

    /// <summary>
    /// Looks up the root span registered for <paramref name="page"/>, or null if none was registered
    /// (e.g. interaction spans are then recorded as their own, disconnected traces).
    /// </summary>
    public static Activity? GetTestRootActivity(IPage page) =>
        TestRootActivities.TryGetValue(page, out var activity) ? activity : null;

    /// <summary>
    /// Ensures the tracer provider is constructed (triggers the static initializer). Safe to call multiple times.
    /// </summary>
    public static void EnsureInitialized() => _ = Provider;

    /// <summary>
    /// Flushes and shuts down the tracer provider, forcing any buffered spans to be exported. Call at the end
    /// of a test run (e.g. from an assembly-level fixture) to avoid losing the last batch of spans.
    /// </summary>
    public static void Shutdown()
    {
        Provider.ForceFlush();
        Provider.Dispose();
    }

    private static TracerProvider BuildTracerProvider()
    {
        var builder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ShopDemo.E2E.Tests", serviceVersion: "1.0.0"))
            .AddSource(SourceName)
            .AddConsoleExporter();

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            builder.AddOtlpExporter();
        }

        var provider = builder.Build();

        // Best-effort flush on process exit, in case a test run ends without calling Shutdown() explicitly.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => provider.ForceFlush();

        return provider;
    }
}
