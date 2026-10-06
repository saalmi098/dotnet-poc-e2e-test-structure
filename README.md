# ShopDemo

A small, client-only webshop proof of concept built with the .NET SDK 10.0.303, Blazor WebAssembly, and MudBlazor 8.14.0. It demonstrates a lightweight separation of domain rules, application use cases, infrastructure, and UI without adding a server, database, ORM, or payment provider.

## Run it

Install a supported .NET 10 SDK, then from the repository root:

```powershell
dotnet restore ShopDemo.sln --source https://api.nuget.org/v3/index.json
dotnet run --project ShopDemo.Client --no-restore
```

Open the local URL printed by the development server. To run the unit tests:

```powershell
dotnet test ShopDemo.Tests --no-restore
```

## Playwright E2E suites

The solution has independent **ShopDemo.E2E.Pom.Tests** and **ShopDemo.E2E.Screenplay.Tests** xUnit v3 projects using `Microsoft.Playwright.Xunit.v3` 1.63.0 and xUnit v3 3.2.2. **ShopDemo.E2E.Shared** contains only the shared Playwright `PageTest` setup, base URL, and demo test data; page objects and Screenplay actors/tasks/questions remain separate. UI controls used by E2E have stable `data-testid` values.

The POM suite uses an xUnit class fixture (`PomPagesFixture`) injected into a common `ShopDemoPomPageTest` base class. Playwright's `PageTest` owns the isolated page/context lifecycle per test; after its async setup, the base test class creates one page-object graph for that test. Test bodies access `Pages.Login`, `Pages.Inventory`, and other objects without repeatedly constructing them. The fixture is a factory and never shares page-bound objects across tests. This is intentionally different from JavaScript Playwright's named fixture injection: xUnit injects class fixtures via constructors, while per-test browser resources remain managed by the .NET `PageTest`.

A lazy `GetPage<T>()` alternative is documented in [`docs/pom-page-object-factory.md`](docs/pom-page-object-factory.md); it is a proposal, not the current POM implementation.

Run both browser suites end-to-end from PowerShell. The script restores from public NuGet, builds the solution, installs Chromium, starts the client, waits for it to respond, runs both suites, and stops the client:

```powershell
pwsh .\scripts\test-e2e.ps1
```

To run a suite against an already-running app, set `SHOPDEMO_BASE_URL` (default `http://127.0.0.1:5178`) and run `dotnet test ShopDemo.E2E.Pom.Tests` or `dotnet test ShopDemo.E2E.Screenplay.Tests`. Each Playwright test uses a fresh browser context; the cross-role test switches users within one context so its session-only stock and orders remain available.

## Demo login

- **Shopper**
  - Email: `demo@test.com`
  - Password: `1234`
- **Store manager**
  - Email: `admin@test.com`
  - Password: `1234`

The role and credentials for both accounts are defined in `ShopDemo.Client/wwwroot/data/demo-users.json`.

## Architecture

- **ShopDemo.Domain** — product, cart, checkout, and order models plus stock/quantity rules.
- **ShopDemo.Application** — use-case interfaces, checkout/order-creation logic, and role-checked inventory management.
- **ShopDemo.Infrastructure** — JSON-backed product and demo-user configuration, and in-memory cart/order state.
- **ShopDemo.Client** — standalone Blazor WebAssembly UI, routing, dependency injection, and MudBlazor components.
- **ShopDemo.Tests** — unit tests for cart rules and checkout behavior.
- **ShopDemo.E2E.Shared** — common Playwright test setup and demo data.
- **ShopDemo.E2E.Pom.Tests** — page objects and POM scenarios.
- **ShopDemo.E2E.Screenplay.Tests** — actors, abilities, tasks, questions, and matching scenarios.

The client loads the sample catalog from `ShopDemo.Client/wwwroot/data/products.json`; its product images use a local placeholder in `wwwroot/images`.

## Included flows

Browse/search/filter products, inspect product details, add/update/remove cart items, submit a simulated checkout, and view session-only orders. Shoppers can only view their own orders; store managers can update stock and inspect all orders created in the current browser session. Sign out and switch roles in the same browser tab to try a manager-restocks / shopper-orders / manager-reviews flow. The tote bag starts out of stock for this scenario.

## PoC and security limitations

This is a **client-only demonstration**, not an ecommerce or authentication system. Product configuration, manager permissions, and demo passwords are shipped to every browser and can be inspected or modified by users. Role checks illustrate UI and application-layer behavior only; they are not security controls. Login state, inventory changes, cart contents, and order history are held in memory and reset on page refresh. Do not put real credentials or sensitive configuration in these files, and never use these demo accounts for real authentication. Checkout only creates an in-memory order; it does not reserve or decrement inventory, charge payment, or persist customer data.
