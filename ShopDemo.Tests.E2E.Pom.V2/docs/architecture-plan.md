# ShopDemo.Tests.E2E.Pom.V2 — Architecture Plan

## Purpose

Build a clean POM proof of concept in a separate `ShopDemo.Tests.E2E.Pom.V2` project. It should test an attribute-bound page-object model for both full pages and nested components, including tab objects.

This file defines the design and implementation plan and records the completed PoC below.

## Design goals

- A page or component object is ready before the factory returns it.
- Locators are declared on properties with attributes and are resolved relative to the object's `BaseLocator`.
- Full pages and nested components follow the same creation and binding rules.
- Objects navigate only through explicit transition methods; actions do not silently move to another page.
- The test code requests only the object needed for the current page or component.
- V2 is designed around these goals, not constrained to follow POM V1's structure.

## Object creation and locator scope

Use a generic reflective factory instead of a `PomPages` wrapper or a central list of page types. The factory creates one requested object, selects its constructor by a documented convention, binds its attributed properties, and returns it only after required locators are visible.

Each page-object constructor receives the Playwright `IPage` and an optional `ILocator? BaseLocator`. Top-level objects normally receive no base locator and resolve their properties from `IPage`. Nested objects receive the parent or tab-container locator, so their properties resolve relative to that container. A top-level object may also receive a base locator when the caller needs to scope it.

```csharp
public abstract class PageObject(IPage page, ILocator? baseLocator = null)
{
    protected IPage Page { get; } = page;
    protected ILocator? BaseLocator { get; } = baseLocator;

    public Task<bool> IsReady() => PageObjectBinder.IsReady(this);
}
```

Define one constructor convention for reflection, such as `(IPage page, ILocator? baseLocator = null)`. Reject ambiguous or unsupported constructors with an error that names the page-object type and the expected signature.

## Locator attributes and binding

Every auto-bound UI property must have exactly one locator attribute: either `[DataTestId]` or `[Locator]`. The attribute is required; it tells the binder how to find the property. The target is required by default and must be visible when the object is created.

Support a dedicated `[DataTestId]` attribute and a generic `[Locator]` attribute. The generic attribute should cover CSS, XPath, text, and role-based Playwright locators. Keep selector strategies explicit and validate attribute/property combinations when binding. Both attributes expose a `Required` named argument that defaults to `true`; use `Required = false` for a conditional target that must not block object creation or `IsReady`.

```csharp
public sealed class ProductDetailsOverviewTab(IPage page, ILocator? baseLocator = null)
    : PageObject(page, baseLocator)
{
    [DataTestId("product-name")]
    public ILocator Name { get; private set; } = null!;

    [Locator(LocatorKind.Role, "button", Name = "Add to cart")]
    public ILocator AddToCart { get; private set; } = null!;

    [Locator(LocatorKind.Css, ".product-card")]
    public ILocator ProductCard { get; private set; } = null!;

    [Locator(LocatorKind.XPath, ".//button[contains(@class, 'add-to-cart')]")]
    public ILocator AddToCartByXPath { get; private set; } = null!;

    [DataTestId("nav-inventory-desktop", Required = false)]
    public ILocator InventoryNavigation { get; private set; } = null!;
}
```

`Name` is only needed for role locators; CSS and XPath use their selector string. `.product-card` is a CSS class selector, and `.//...` keeps the XPath relative to the current locator scope.

The binder walks public attributed properties, creates each locator from `BaseLocator` when present (otherwise from `IPage`), and waits for required locators to be visible. Nested page-object properties use their attributed container locator as the child's `BaseLocator`; binding continues recursively. A `Required = false` locator is still assigned, but does not participate in the initial visible wait or readiness check; tests can still query its visibility or presence.

Required properties participate in readiness and fail initialization with a clear timeout/error when missing or hidden. `IsReady` returns `Task<bool>` and checks the current state of required bound locators. It does not navigate or make a page ready.

## Tabs and nested objects

Create a tab object only after its tab has been activated. The parent page object's (e.g. `SwitchToOverviewTab`) method clicks the existing tab control, waits for the panel, and asks the factory to create the tab object with the panel locator as `BaseLocator`. Do not add test-specific tab-switching behavior to the application.

Extend `ShopDemo.Client\POM\ProductDetails.razor` with a normal two-tab UI to exercise the pattern:

- **Overview** — product name, description, category, and price.
- **Inventory** — product ID, stock, and availability.

Implement the tabs using MudBlazor (`<MudTabs>`). Use stable `data-testid` values for tab controls, panel roots, and fields. These fields map to the existing `Product` model. The POM activates tabs; the application provides only the normal tab UI and content.

## Conditional and role-specific objects

Use the smallest model that fits the variation:

- For a few conditional elements, set `Required = false`. The current manager-only inventory navigation item is this kind of variation.
- When many fields or behaviors differ by role, use concrete page-object types derived from a shared base, such as `AdminAccountPage` and `UserAccountPage`.
- Explicit role-aware navigation or sign-in methods choose and return the concrete type. Do not make the binder guess the user's role.

There is no fixed numeric threshold. The implementation should explain the choice based on how much behavior and locator state is shared.

## Navigation and method naming

A transition method performs the navigation and returns a newly created, initialized object:

```csharp
public async Task<ProductDetailsOverviewTab> SwitchToOverviewTab()
{
    await OverviewTabButton.ClickAsync();
    var panel = Page.GetByTestId("product-overview-panel");
    return await _factory.Create<ProductDetailsOverviewTab>(Page, panel);
}
```

Use `Task`/`Task<T>` for asynchronous Playwright work, but omit the `Async` suffix from method names. For example, use `Create`, `SwitchToOverviewTab`, and `NavigateToDetails`.

Page-object methods assume the object represents the current page or active component. They must not silently navigate to a page before performing an action.

## Test scope

The V2 project should cover the same four existing journeys:

1. A manager restocks an item, a shopper places an order, and the manager reviews it.
2. A guest is redirected to login from inventory management.
3. A shopper can view their own order history.
4. A shopper does not see inventory-management navigation.

Add a focused tab journey: open product details, switch to both tabs through the parent page object, and verify that each returned tab object exposes its expected fields. This checks scoped locators, explicit activation, and ready-on-creation behavior.

## Implementation outline

1. Create the separate `ShopDemo.Tests.E2E.Pom.V2` project. Reuse neutral browser setup or test data only when it helps; do not inherit V1 page objects or preserve its wrapper structure for compatibility.
2. Implement the locator attributes, strategy resolver, reflective constructor selection, binder, and required/optional readiness behavior.
3. Add a base page-object type with optional `BaseLocator`, plus the generic creation path for top-level and nested objects.
4. Implement explicit page transitions and parent-owned tab switching that returns a bound tab object.
5. Add the Product Details tabs and stable test IDs, without application-specific test hooks for switching tabs.
6. Implement the four existing journeys and the tab-binding journey in the V2 test project.
7. Verify the build and run the V2 E2E suite against all five journeys.

## Acceptance criteria

- Full-page and nested page objects use the same attribute-binding lifecycle.
- `BaseLocator` is optional for top-level objects and scopes nested objects.
- Required locators are visible before creation returns; `Required = false` locators do not block creation or readiness.
- Tab objects are created only after the parent object activates the tab.
- Transitions return ready page objects, and ordinary actions never auto-navigate.
- Product Details has two meaningful tabs with test IDs, and the tab journey verifies both bound object shapes.
- All four existing journeys and the new tab journey pass in V2.
- V2 remains an independent PoC; V1 compatibility is not an acceptance criterion.

## Exceptions and trade-offs to record

- Playwright locator operations are asynchronous, so construction cannot complete binding in a normal synchronous constructor. The factory returns `Task<T>`; method names intentionally omit `Async`.
- A required-visible-by-default rule conflicts with hidden tabs and role-dependent controls. Tab objects are deferred until activation; only a small number of conditional controls use `Required = false`.
- Reflection reduces manual wiring but moves some mistakes to runtime. Constructor conventions, unsupported property types, and selector errors must fail explicitly with actionable messages.
- Role variation has no fixed cutoff. Record the reason whenever the design chooses `Required = false` properties or separate derived types.
- The new Product Details tabs are application UI added to make the POM pattern meaningful. Do not add application logic solely to help tests switch tabs.
- Document any additional exceptions discovered during implementation, including the reason and the default rule being changed.

## Implementation record

The V2 project is implemented as a separate xUnit v3 project and references only `ShopDemo.Tests.E2E.Shared`; it does not reference V1 or reuse its page objects or wrapper. The reflective factory enforces the public `(IPage page, ILocator? baseLocator = null)` constructor convention. The binder supports `DataTestId` plus role, text, CSS, and XPath locators; it recursively binds nested page-object properties relative to their declared container locator. Required locators are awaited for visibility and included in `IsReady`; optional locators are still assigned but omitted from both checks. Binding failures identify the object, property, and selector.

V2 uses optional locators on the shared navigation component because login, logout, orders, cart, and manager-only inventory links vary with authentication and role. The catalog link and the navigation container remain required. Product tab objects are created only after their parent clicks the corresponding normal MudBlazor tab; their fields are scoped to the visible panel. MudBlazor applies unmatched `MudTabPanel` attributes to the panel (`role=tabpanel`), not its clickable tab, so each stable tab test ID is placed on a label span rendered inside the normal tab control; the E2E test verifies that it is within a `role=tab`. The four existing journeys and a two-tab product-details journey are implemented, along with focused factory tests covering all locator strategies, nested scope, optional fields, readiness, and useful required-binding errors.

The only behavior added to application UI is the planned Product Details tab presentation and stable test IDs for its controls/fields and the catalog Details link. No test-only tab switching hook was added. The label-span placement is the only implementation exception; it accommodates MudBlazor's attribute forwarding without changing the architecture's tab activation or scoping rules.
