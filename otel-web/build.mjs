// Bundles src/tracing.js (ESM, depends on npm packages) into a single self-contained script Blazor can serve
// as a static file, since Blazor WebAssembly has no npm/bundler step of its own.
import * as esbuild from "esbuild";

const outfile = "../ShopDemo.Client/wwwroot/js/otel-web-tracing.js";

await esbuild.build({
  entryPoints: ["src/tracing.js"],
  bundle: true,
  minify: true,
  sourcemap: true,
  format: "iife",
  target: ["es2020"],
  outfile,
});

console.log(`Built ${outfile}`);
