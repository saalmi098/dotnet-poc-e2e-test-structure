// Browser-side OpenTelemetry instrumentation for ShopDemo.Client.
//
// Unlike the C#-side instrumentation in ShopDemo.Tests.E2E.Otel/ShopDemo.Tests.E2E.Pom.V4 (which only
// produces spans when a test goes through the typed Page Object Model), this listens to real DOM events
// (click, submit, page load) directly in the browser. It traces what actually happened on the page, so it
// still works for a test that drives Playwright with raw locators and no Page Object Model at all, or one an
// LLM generated incorrectly - exactly the gap the POM-based instrumentation can't cover. See the README's
// "UI interaction tracing" section for how the two compare.
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { SimpleSpanProcessor, ConsoleSpanExporter } from "@opentelemetry/sdk-trace-base";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { UserInteractionInstrumentation } from "@opentelemetry/instrumentation-user-interaction";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from "@opentelemetry/semantic-conventions";

// Overridable via `window.__OTEL_EXPORTER_OTLP_ENDPOINT__` set in index.html before this script runs (e.g. for
// a different collector host in CI). Must be an OTLP/HTTP endpoint the browser can reach directly - not
// Jaeger's OTLP port, which doesn't send CORS headers; see docker-compose.yml's otel-collector service.
const otlpEndpoint = window.__OTEL_EXPORTER_OTLP_ENDPOINT__ ?? "http://localhost:4319/v1/traces";

const provider = new WebTracerProvider({
  resource: resourceFromAttributes({
    [ATTR_SERVICE_NAME]: "ShopDemo.Client (browser)",
    [ATTR_SERVICE_VERSION]: "1.0.0",
  }),
  spanProcessors: [
    // Console output makes this visible without Docker running, same as the C#-side instrumentation.
    new SimpleSpanProcessor(new ConsoleSpanExporter()),
    new SimpleSpanProcessor(new OTLPTraceExporter({ url: otlpEndpoint })),
  ],
});

provider.register({
  // Zone.js-backed context propagation: without it, a span started on a click handler wouldn't stay
  // "current" across the async work that handler triggers (e.g. a Blazor re-render), so nested spans
  // wouldn't nest correctly.
  contextManager: new ZoneContextManager(),
});

registerInstrumentations({
  tracerProvider: provider,
  instrumentations: [
    new UserInteractionInstrumentation({
      eventNames: ["click", "submit"],
    }),
    new DocumentLoadInstrumentation(),
  ],
});

console.info("[otel-web] browser tracing initialized, exporting to", otlpEndpoint);
