# ShopDemo

A full-featured demonstration webshop that serves as the **system under test** for comparing end-to-end test structures (Baseline, Page Objects V1-V4, Screenplay) and for assessing LLM-generated Playwright tests. It is built with .NET SDK 10, an ASP.NET Core minimal API, EF Core (SQLite or PostgreSQL), and a Blazor WebAssembly + MudBlazor 8.14.0 client. Every important UI element carries a stable `data-testid`; see [`docs/application.md`](docs/application.md) for the feature list, test-id catalog, and API reference.

## Run it

### Locally (SQLite, no extra tooling)

```powershell
dotnet restore ShopDemo.sln --source https://api.nuget.org/v3/index.json
dotnet run --project ShopDemo.Server --no-restore
```

The server hosts the API and the Blazor client; open the URL printed on start-up (`https://localhost:53246` / `http://localhost:53247` with the launch profile). The SQLite file is created at `ShopDemo.Server/App_Data/shopdemo.db` and filled with demo data on first start. Delete the file, or call `POST /api/test/reset`, to start over.

### With Docker Compose (PostgreSQL + API)

```powershell
docker compose up --build        # http://localhost:5178
docker compose down --volumes    # stop and delete the database
```

`docker-compose.yml` starts `db` (PostgreSQL 17, port 5432, user/password/database `shopdemo`) and `app` (the published server image, port 5178; override with `SHOPDEMO_PORT`). The database schema is created and seeded by the app on start-up; the app waits for the database health check. To run the browser suites against the stack: `pwsh ./scripts/test-e2e.ps1 -Docker`.

### Configuration

| Setting | Default | Meaning |
| --- | --- | --- |
| `Database__Provider` | `Sqlite` | `Sqlite` or `Postgres` |
| `ConnectionStrings__ShopDb` | `Data Source=App_Data/shopdemo.db` | Connection string for the provider |
| `Testing__EnableResetEndpoint` | `true` | Enables `POST /api/test/reset` (restores the seed data); disable for anything that is not a test target |

Unit and service tests (domain rules, EF-backed services on in-memory SQLite):

```powershell
dotnet test ShopDemo.Tests --no-restore
```

## Playwright E2E suites

The solution has six independent xUnit v3 projects using `Microsoft.Playwright.Xunit.v3` 1.63.0 and xUnit v3 3.2.2: **ShopDemo.Tests.E2E.Baseline** uses Playwright directly, **ShopDemo.Tests.E2E.Pom.V1** uses Page Objects, **ShopDemo.Tests.E2E.Pom.V2** is an attribute-bound Page Object PoC, **ShopDemo.Tests.E2E.Pom.V3** is a compile-time (reflection-free) Page Object PoC, **ShopDemo.Tests.E2E.Pom.V4** extends V2 with typed MudBlazor control wrappers (work in progress), and **ShopDemo.Tests.E2E.Screenplay** uses Screenplay. V4 bound properties use `required init` rather than null-forgiving initializers. Locators are required by default; `[Locator(..., Optional = true)]` skips the initial visibility wait and owning page's readiness requirement without making the bound locator or control property nullable. See the [V4 test-creation guide](ShopDemo.Tests.E2E.Pom.V4/docs/test-creation-guide.md) for examples. The original three suites implement the same four journeys (the application has since grown many more features, see `docs/application.md`); V2 and V3 cover those journeys plus a product-details tab journey. **ShopDemo.Tests.E2E.Shared** contains only the shared Playwright `PageTest` setup, base URL, and demo test data. UI controls used by E2E have stable `data-testid` values.

The Baseline suite keeps navigation, selectors, and interactions directly in each test to provide a comparison for code length and duplication. The POM V1 suite uses `PomPageTestBase`, which inherits `ShopDemoPageTest` to reuse its browser context options and provides a lazy `Pages` property. The page-object graph is created on first access and belongs to that test's `PageTest.Page`; no page-bound objects are shared across tests.

A separate V2 project deliberately does not reference V1. Its reflective factory injects `IPage` and an optional locator scope, while its binder resolves `[DataTestId]` and `[Locator]` attributes and waits for required locators before returning objects. It supports nested component scopes and defers tab-object creation until the parent clicks the tab. The Product Details page uses regular MudBlazor Overview and Inventory tabs.

A lazy `GetPage<T>()` alternative is documented in [`docs/pom-page-object-factory.md`](docs/pom-page-object-factory.md); it is a proposal, not the current POM implementation.

A separate V3 project also does not reference V1 or V2. It covers the same five journeys as V2 with no reflection: a static-abstract interface member (`IPageComponent<T>`) lets `PageComponent.Create<T>` construct and wait for any page object at compile time, and required/optional UI is a property type (`Required<ILocator>`/`Optional<ILocator>`) assigned once in the constructor rather than an attribute resolved by a binder. See [`ShopDemo.Tests.E2E.Pom.V3/docs/architecture.md`](ShopDemo.Tests.E2E.Pom.V3/docs/architecture.md) for the approach and its strengths/drawbacks versus V2.

Run all browser suites end-to-end from PowerShell. The script restores from public NuGet, builds the solution, installs Chromium, starts the server on a temporary SQLite database (or the docker compose stack with `-Docker`), waits for `/api/health`, runs the unit tests and all browser suites, and stops everything:

```powershell
pwsh .\scripts\test-e2e.ps1
```

To run a suite against an already-running app, set `SHOPDEMO_BASE_URL` (default `http://127.0.0.1:5178`) and run `dotnet test ShopDemo.Tests.E2E.Baseline`, `dotnet test ShopDemo.Tests.E2E.Pom.V1`, `dotnet test ShopDemo.Tests.E2E.Pom.V2`, `dotnet test ShopDemo.Tests.E2E.Pom.V3`, `dotnet test ShopDemo.Tests.E2E.Pom.V4`, or `dotnet test ShopDemo.Tests.E2E.Screenplay`. Each Playwright test uses a fresh browser context and starts from freshly seeded data.

### Reuse in the test patterns

- **POM V1:** For example `LoginPage.SignInAsync` and `CheckoutPage.PlaceOrderAsync` are reused in multiple shopper/manager flows; each page object keeps its UI actions and selectors together.
- **Screenplay:** For example `SignIn`, `AddProductToCart`, and `CompleteCheckout` are composed in multiple scenarios. The same `OrderIsVisible` question checks their outcomes.
- **Baseline:** The same login, cart, and checkout interactions are repeated inline, showing what the abstractions remove.

## Demo accounts

All demo accounts use the password `1234` (accounts you register need 8+ characters).

| Role | Email | Notes |
| --- | --- | --- |
| Shopper | `demo@test.com` | Has a saved address, one delivered order, one wishlist item |
| Store manager | `admin@test.com` | Dashboard, inventory, products, coupons, order management |
| Shopper | `jane@test.com` | Two seeded orders (delivered, shipped) - useful for "only my orders" checks |

Demo coupons: `WELCOME10` (10 % off), `SAVE5` (5.00 off from 30.00), `FREESHIP` (free shipping), `OLD20` (expired), `ONCE` (already fully redeemed). Test card numbers: `4242 4242 4242 4242` is accepted, `4000 0000 0000 0002` is declined.

## Architecture

- **ShopDemo.Domain** - entities, enums, pricing/coupon/order-status rules, `ShopException`.
- **ShopDemo.Contracts** - request/response DTOs shared by server and client.
- **ShopDemo.Infrastructure** - EF Core `ShopDbContext` (SQLite or PostgreSQL), seeder, and the use-case services (account, catalog, wishlist, cart, orders, admin).
- **ShopDemo.Server** - ASP.NET Core minimal API (cookie authentication, role policy), static hosting of the Blazor client, test-support endpoint.
- **ShopDemo.Client** - Blazor WebAssembly UI with MudBlazor; talks to the API through the typed `ShopApi`.
- **ShopDemo.Tests** - unit tests for pricing/order rules and service tests against in-memory SQLite.
- **ShopDemo.Tests.E2E.Shared** - common Playwright test setup, demo data, and `TestDatabase.ResetAsync()`.
- **ShopDemo.Tests.E2E.Baseline** - direct Playwright tests without POM or Screenplay abstractions.
- **ShopDemo.Tests.E2E.Pom.V1** - page objects and POM scenarios.
- **ShopDemo.Tests.E2E.Pom.V2** - independent attribute-bound page objects, reflective factory, locator binder, and tab scenarios.
- **ShopDemo.Tests.E2E.Pom.V3** - independent compile-time page objects: static-abstract-interface-based generic creation, `Required<T>`/`Optional<T>` typed locators, no reflection.
- **ShopDemo.Tests.E2E.Pom.V4** - V2 plus typed control wrappers (`Button`, `TextField`, `Select`, ...) that encapsulate MudBlazor interaction behaviour; see the [test-creation guide](ShopDemo.Tests.E2E.Pom.V4/docs/test-creation-guide.md); work in progress.
- **ShopDemo.Tests.E2E.Screenplay** - actors, abilities, tasks, questions, and matching scenarios.

## Test isolation

State lives in the database, so the browser suites call `POST /api/test/reset` before every test (`ShopDemoPageTest` / `ShopDemoPageTestV4`) and run without parallelization. Set `SHOPDEMO_RESET_DB=false` to keep data between tests. Lists are filled from the API after navigation; assert with retrying expectations (or `LocatorExtensions.IsEventuallyVisibleAsync`) instead of reading a count immediately after a click.

## Security and scope notes

This is a **demonstration application**. Passwords are hashed (ASP.NET Core Identity password hasher) and the session is an HttpOnly, SameSite=Lax cookie, but there is no email verification, rate limiting, CSRF token, or real payment provider; card handling is simulated and only the last four digits are stored. The seeded accounts use a trivial password, and `POST /api/test/reset` wipes all data - never expose either outside a test environment.
