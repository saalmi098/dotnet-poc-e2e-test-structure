# E2E Test Structure PoC

**ShopDemo · Page Object Model, Screenplay, and their evolution**

> **In one sentence:** The PoC compares direct Playwright tests with progressively more reusable test abstractions, using the same webshop journeys to make the trade-offs visible.

## What is being compared?

The repository contains independent xUnit/Playwright E2E projects for the same ShopDemo app:

- **Baseline** — Playwright selectors and actions are written directly in each test.
- **POM V1, V2, and V4** — a mainline progression from hand-written page objects to bound page objects and typed UI controls.
- **POM V3** — an alternative, compile-time approach; see [Appendix: POM V3](#appendix-pom-v3--compile-time-alternative).
- **Screenplay** — tests describe actors, tasks, and questions instead of pages.

All suites use shared browser setup and demo data from `ShopDemo.Tests.E2E.Shared`. The key business journey is the same: a manager restocks a product, a shopper orders it, and the manager finds the order.

```text
Manager signs in ──> Restocks product ──> Signs out
                                           │
Shopper signs in ──> Adds product ──> Checks out ──> Order number
                                                         │
Manager signs in ──> Opens orders ──> Finds that order ──┘
```

## Baseline: what hurts?

In the baseline test, the journey is expressed as a long sequence of low-level UI details:

```csharp
await Page.GetByTestId("nav-login-desktop").ClickAsync();
await Page.GetByTestId("login-email").FillAsync(account.Email);
await Page.GetByTestId("login-password").FillAsync(account.Password);
await Page.GetByTestId("login-submit").ClickAsync();
await Page.GetByTestId("nav-logout-desktop").WaitForAsync();
```

The same login, navigation, and checkout steps appear again in other tests. The result is:

- **Readability:** business intent is buried in selectors, waits, and clicks.
- **Maintainability:** a UI change can require edits in many tests.
- **Reusability:** repeated workflows are copied instead of named and shared.
- **Consistency:** each test can implement the same action or wait differently.
- **Failure diagnosis:** tests often fail at a low-level interaction rather than at a meaningful page or task boundary.

The baseline is useful as a reference point: it has little framework overhead, and clearly exposes what the browser does. The problem is duplication as scenarios grow.

## Two ways to structure E2E tests

### Page Object Model (POM)

POM groups UI locators and interactions by page or page component. A test asks a page object to perform an operation; the page object hides the UI mechanics.

```csharp
await Pages.Login.SignInAsync(manager);
await Pages.Inventory.UpdateStockAsync(productId, 4);
```

```text
Test ──calls──> Page objects ──use──> Playwright / UI
       business flow              locators, clicks, waits
```

**Good fit when:** the UI has stable pages/components, and teams want selector changes localized near the relevant UI.

**Watch for:** page objects becoming large, test intent leaking into UI helpers, or factories/binders adding more machinery than the suite needs.

### Screenplay

Screenplay organizes tests around **actors** with **abilities**, who perform **tasks** and ask **questions**. A task captures a meaningful action; a question captures an observation.

```csharp
await shopper.AttemptsToAsync(
    new SignIn(shopperAccount),
    new AddProductToCart(productId),
    new CompleteCheckout("Sample Shopper", shopperEmail));

var orderNumber = await shopper.AsksAsync(new ConfirmedOrderNumber());
```

```text
Actor ──has──> Ability (BrowseTheWeb)
  │
  ├── performs ──> Tasks (SignIn, CompleteCheckout)
  └── asks ──────> Questions (ConfirmedOrderNumber)
```

**Good fit when:** scenarios benefit from a clear business vocabulary or the same tasks need to be composed across different actors and journeys.

**Watch for:** extra concepts and indirection. For a small suite, actors/tasks/questions may be more structure than the tests need.

| | POM | Screenplay |
|---|---|---|
| Organizes around | Pages and components | Actors, abilities, tasks, questions |
| Typical test reads like | “Open catalog; add product” | “Shopper performs checkout” |
| Reuse boundary | UI page/component operation | User goal or observable outcome |
| Main trade-off | Page objects can grow too broad | More concepts and indirection |

Neither pattern is universally better. They emphasize different boundaries; both can make intent clearer and reduce duplication when applied consistently.

## POM evolution in this repository

The implemented POM progression is **Baseline → V1 → V2 → V4**. V4 is the newest POM project in the solution and builds on V2. V3 is a separate alternative, not a step between V2 and V4.

```text
Baseline ──> V1 ──> V2 ──> V4
                         └──> V3 (alternative compile-time design)
```

### V1 — hand-written page objects

- Page classes own their selectors and common actions, such as `LoginPage.SignInAsync`.
- Tests become shorter and describe the flow in terms of pages.
- `PomPages` provides a convenient, fixed set of page objects for the test.
- **Trade-off:** adding a page means updating the central collection; page objects still contain manually written Playwright selectors and waits.

```csharp
await Pages.Login.SignInAsync(E2ETestSettings.StoreManager);
await Pages.Inventory.UpdateStockAsync(E2ETestSettings.CanvasToteId, 4);
```

### V2 — attribute-bound page objects

- `[DataTestId]` and `[Locator]` metadata declare how properties map to UI elements.
- `PageObjectFactory` constructs objects; a reflective binder resolves attributes, assigns locators, and waits for required elements.
- Nested page objects can be scoped to a parent locator. This supports components and deferred tab content.
- **Added value:** less repetitive locator setup and a consistent “required elements are ready” rule.
- **Trade-off:** reflection and attributes shift errors toward runtime; the factory, binder, and locator resolver add framework complexity.

```csharp
public sealed class LoginPage(IPage page, ILocator? baseLocator = null)
    : PageObject(page, baseLocator)
{
    [DataTestId("login-email")]
    public ILocator Email { get; private set; } = null!;

    public async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Email.FillAsync(account.Email);
        // Password, submit, and readiness handling omitted.
        return await Create<ShopNavigation>();
    }
}
```

### V4 — typed controls on the V2 foundation

- Keeps V2's attribute binding, factory, and page-object structure.
- Adds reusable controls such as `TextField`, `Button`, `Select`, and `DataGrid`.
- Encapsulates recurring interaction details and MudBlazor-specific locator behavior.
- Tests can use controls directly and keep Playwright's retrying assertions where useful.
- **Added value:** common control behavior is centralized and test calls become more intention-revealing.
- **Trade-off / status:** another abstraction layer must be maintained. The project is marked as a work in progress.

```csharp
public sealed class LoginPage(IPage page, ILocator? baseLocator = null)
    : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "login-email")]
    public TextField Email { get; private set; } = null!;
}

await login.Email.Fill(account.Email);
```

### What the progression demonstrates

```text
More duplicated UI detail                         More abstraction
Baseline  ───────────────>  V1  ───────────────>  V2  ───────>  V4
inline selectors             named page actions    auto-binding   typed controls
```

- **Baseline → V1:** makes repeated UI operations reusable and tests more readable.
- **V1 → V2:** reduces manual locator wiring and adds scoped components/readiness.
- **V2 → V4:** gives common UI widgets their own interaction APIs.
- Each step trades some directness for more reuse and framework code. Add a layer only when its consistency or reuse repays that cost.

## Where Screenplay fits

The Screenplay project is a parallel comparison, not a POM version. Its manager/shopper journey uses two actors and composes tasks such as `SignIn`, `RestockProduct`, and `CompleteCheckout`. A question such as `OrderIsVisible` expresses the outcome independently of the journey's wording.

Choose **POM** when the team wants UI structure and selectors grouped by page/component. Choose **Screenplay** when the team wants reusable business-level actions and explicit user roles. The repository demonstrates both; it does not claim one is universally superior.

## Appendix: POM V3 — compile-time alternative

V3 explores the same goals as V2 but uses a different mechanism:

- `IPageComponent<T>` and static abstract members create typed components without reflection.
- `Required<T>` and `Optional<T>` make locator optionality visible in the property type.
- `PageComponent.Create<T>` constructs the object and waits for readiness.
- **Strengths:** compiler-checked construction, no attribute binder, smaller framework.
- **Trade-offs:** `.Value` is required to access wrapped locators; each page object has construction boilerplate; component scoping is by convention rather than enforced by the binder.

```text
V2: attributes + runtime reflection + structural locator binding
V3: typed properties + compile-time construction + convention-based scope
```

V3 is best read as an alternative design experiment, not as a direct continuation of V2. Its dedicated notes are in [`ShopDemo.Tests.E2E.Pom.V3/docs/architecture.md`](../ShopDemo.Tests.E2E.Pom.V3/docs/architecture.md).

## Source map

- [Baseline journey](../ShopDemo.Tests.E2E.Baseline/ManagerRestocksShopperOrdersTests.cs)
- [V1 journey and page objects](../ShopDemo.Tests.E2E.Pom.V1/ManagerRestocksShopperOrdersTests.cs)
- [V2 binder and page objects](../ShopDemo.Tests.E2E.Pom.V2/Infrastructure/POM/PageObjectBinder.cs)
- [V4 typed controls](../ShopDemo.Tests.E2E.Pom.V4/Infrastructure/Controls)
- [Screenplay journey](../ShopDemo.Tests.E2E.Screenplay/ManagerRestocksShopperOrdersTests.cs)
- [Repository overview and run instructions](../README.md)
