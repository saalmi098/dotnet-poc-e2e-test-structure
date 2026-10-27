# POM V4 Architecture

POM V4 organizes tests around page objects that describe the application and typed controls that encapsulate reusable UI interactions. This keeps test journeys focused on user behavior, reduces repeated selector and interaction code, and isolates MudBlazor-specific details. Playwright locators are scoped and remain lazy until used; required properties are additionally checked for visibility when an object is initialized.

## Page objects and journeys

Page objects are the primary interface for tests: they expose meaningful application fields and operations rather than raw selectors. They primarily expose UI elements through attributed properties; each locator attribute describes how to find its element, and the factory binds it when creating the page object. This keeps selectors close to the page-object fields that represent them, while tests use those meaningful fields and operations.

For example, `ProductDetailsPage` owns tab activation. It selects a tab, then creates and returns the corresponding page object scoped to that tab's panel. The tab page objects expose their fields and grid controls within the active panel.

> Note: Controls whose selectors depend on runtime data cannot be declared as fixed attributed properties. They are constructed directly with scoped locators, as in the product-specific buttons and stock fields in `CatalogPage` and `InventoryPage`.

## Typed controls

Typed controls wrap Playwright locators so common actions and assertions can be reused without repeating low-level interaction details in every page object. They also provide a place to handle application-specific behavior, such as MudBlazor's select popovers and tab markup.

Controls expose their resolved root as `Locator` for direct Playwright APIs and `Expect()` for Playwright assertions. They do not implement `ILocator`, because Playwright's `Expect(ILocator)` expects its own locator implementation.

| Control | Encapsulated behavior |
|---|---|
| `Button` | Click and read button text |
| `TextField` / `NumericField` | Fill and read input values |
| `Select` | Resolve the visible MudBlazor control, choose an exact-text option from its page-level popover, and read the selection |
| `Tab` | Resolve the enclosing tab and select it, waiting for `aria-selected="true"` |
| `DataGrid` | Expose column headers, displayed rows, and cells |
| `DataGridPager` | Navigate pages and change rows per page, waiting for the displayed range to change |

`ResolveControlRoot` lets a control map its matched locator to the element that should be interacted with and checked for visibility. Most controls use the matched locator directly. For example, MudBlazor may put a select's test ID on a hidden input; the visible control container is the appropriate readiness and interaction target.

## Locator declarations

`LocatorAttribute` declares how a page-object property finds its UI element. `LocatorKind` selects one of five strategies:

| Kind | `Selector` value | Resolution |
|---|---|---|
| `DataTestId` | The `data-testid` value | Playwright `GetByTestId` |
| `Role` | An ARIA role, such as `button` | `GetByRole`, optionally filtered by `Name` |
| `Text` | The exact text to match | `GetByText` with exact matching |
| `Css` | A CSS selector | `Locator` with a CSS selector |
| `XPath` | An XPath expression | `Locator` with an XPath selector |

Example declarations:

```csharp
[Locator(LocatorKind.DataTestId, "login-email")]
public required TextField Email { get; init; }

[Locator(LocatorKind.Role, "button", Name = "Save")]
public required Button Save { get; init; }
```

Locators are required by default. `Optional = true` still binds the locator or control but skips its initial visibility wait and does not make it a readiness requirement of the owning page object. `Name` is supported only for role locators. Role accessible-name and text matching are always exact; there is no attribute option to weaken that behavior.

## Object creation, binding, and readiness

`PageObjectFactory` is the public entry point and creates a fresh `ComponentTreeBuilder` for each root page object. The tree builder owns the recursive operation and delegates construction to `ComponentActivator`; `LocatorResolver` validates locator metadata and creates scoped Playwright locators. This separates construction, metadata resolution, and recursive binding while keeping binding state isolated to one factory call.

Application test classes derive from `ShopDemoPageTestV4<TPageObject>` and declare a `PageUrl`. During test initialization, the base navigates to that route, creates and binds the typed page object with its per-test factory, and exposes it as `Model`. This removes repeated initial `Page.GotoAsync` and `CreatePageObject<T>()` calls. Keep each class to one initial route and model; split tests into separate classes when those differ. The non-generic `ShopDemoPageTestV4` remains available for factory-focused tests and cases that need to create page objects explicitly. Each `PageObject` owns a `PageObjectFactory` for creating nested or destination page objects; these are separate factory instances. Tests should use returned objects for transitions instead of recreating page objects for the current route.

Factory-created types must expose exactly one public constructor with the signature `(IPage page, ILocator? baseLocator = null)`. The binder processes attributed public properties only. They must be `ILocator` or `IBindableComponent` properties with a setter and carry at most one `LocatorAttribute`; invalid declarations cause binding errors. Unattributed properties are ignored.

`LocatorResolver` resolves each attribute using the supplied `baseLocator` as its scope, or the page when no base is given. Resolved Playwright locators stay lazy until an operation or wait uses them. During tree construction, required `ILocator` properties wait for their matched locator to become visible. For controls, the tree builder constructs the control first and waits on its resolved root instead. It detects cyclic component metadata on the active recursion path, while allowing the same component type in separate sibling scopes.

Page objects and controls are separate class hierarchies connected by `IBindableComponent`, the contract used by the factory and tree builder to register required locators and check readiness. Both delegate their shared page, optional parent `ILocator` scope, and required-locator state to `ComponentBindingContext`; page objects additionally track required child components and check readiness recursively. Controls are leaves; required component children on controls are rejected.

Binding readiness is an initial wait, not navigation. The generic test base binds `Model` before the test body runs, waiting for required locators. `IsReady()` is a non-blocking check of current visibility; it can assert readiness but does not wait for the UI to become ready. Page-object actions do not silently navigate.

## Tests and coverage

`ShopNavigationJourneyTests`, `CatalogPageTests`, `LoginPageTests`, `GuestInventoryRedirectTests`, and `ProductDetailsTests` exercise application journeys and typed controls against the running ShopDemo application. The test classes use the generic base with the page object and route appropriate to their initial state. Live page-object attributes primarily use test IDs; `ShopNavigation` also binds its app bar with CSS.

`PageObjectFactoryTests` covers factory construction, failure isolation, concurrent calls, and scoped destination creation. `ComponentTreeBuilderTests` uses browser-rendered `SetContentAsync` markup to exercise recursive binding, parent scoping, required and optional readiness, control-root resolution, and binding failures. `LocatorResolverTests` covers locator strategies, metadata validation, and role validation; live page-object attributes primarily use test IDs and CSS.

## Relationship to V2

V4 retains V2's reflective factory, locator resolver, scoped page objects, and required/optional binding model. It adds the unified `LocatorAttribute` (including test IDs as a locator kind), typed control wrappers, and MudBlazor-specific control-root and interaction behavior. Required-child readiness is owned by `PageObject`; controls remain leaf components.
