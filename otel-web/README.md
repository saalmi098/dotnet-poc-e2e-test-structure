# otel-web

Builds `ShopDemo.Client/wwwroot/js/otel-web-tracing.js`: a self-contained, minified bundle of the
[OpenTelemetry Web SDK](https://opentelemetry.io/docs/languages/js/) that auto-instruments real DOM events
(`click`, `submit`, page load) in the running Blazor app.

## Why this exists alongside the C#-side instrumentation

`ShopDemo.Tests.E2E.Otel` (see the main README's "UI interaction tracing" section) only produces a span when
a test goes through the typed Page Object Model (`Button.Click()`, `TextField.Fill()`, ...). That's fine for
hand-written POM tests, but it has a gap: a test written against raw Playwright locators — or one an LLM
generated without using the POM correctly — produces *no* spans at all from that side.

This bundle closes that gap from the other end: it listens to the actual browser DOM, not to C# code paths.
It traces whatever really happened on the page, regardless of how the test that drove the browser was
written. Load both app and 46 V4 tests + browser instrumentation, and you get two independent views of the
same session that you can compare directly in Jaeger (two different `service.name`s:
`ShopDemo.E2E.Tests` for the C# side, `ShopDemo.Client (browser)` for this one).

## Rebuild

```powershell
cd otel-web
npm install
npm run build
```

Commit the generated `../ShopDemo.Client/wwwroot/js/otel-web-tracing.js` (and its `.map`) — Blazor WebAssembly
has no npm/bundler step of its own, so the build output is checked in like any other static asset.

## Where spans go

Exports to the console (open the browser dev tools) and, via OTLP/HTTP, to
`http://localhost:4319/v1/traces` by default — the `otel-collector` service in `../docker-compose.yml`, not
Jaeger's own OTLP port directly. Jaeger's OTLP receiver doesn't send CORS headers, so a browser `fetch()`
straight to it gets blocked by the browser; the collector sits in front with CORS enabled for the app's own
origin and forwards to Jaeger. Override the endpoint by setting `window.__OTEL_EXPORTER_OTLP_ENDPOINT__`
before this script runs (e.g. in `index.html`, for a different collector host in CI).

## Dependency versions

Pinned to exact versions (not `^`) with an `overrides` block forcing `@opentelemetry/api` and
`@opentelemetry/instrumentation` tree-wide. The OpenTelemetry JS packages were mid-migration to a new major
version (`@opentelemetry/api@1.10`, `@opentelemetry/resources@3.x`, `instrumentation@0.300.x`) at the time
this was built, and `@opentelemetry/sdk-trace-web`/`sdk-trace-base` hadn't caught up yet (still on the
`1.9`-compatible `2.12.0` line) — mixing the two epochs makes esbuild fail with "No matching export ... for
import 'logs'" because nested copies of `@opentelemetry/instrumentation` disagree on what `@opentelemetry/api`
exports. The pins + overrides keep the whole tree on one consistent (pre-`0.300`) epoch. Revisit once the
ecosystem settles on the new major across all these packages.
