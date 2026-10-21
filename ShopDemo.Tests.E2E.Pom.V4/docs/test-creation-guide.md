**// TODO: document needs to be reviewed**

# Adding tests to ShopDemo POM V4

Use this guide when adding an end-to-end journey or the page objects and controls it needs. For the design and component model, see [architecture.md](architecture.md).

## Project boundary and structure

POM V4 is an independent xUnit v3 project. It references `ShopDemo.Tests.E2E.Shared` for shared test settings, the base URL, and demo accounts. Keep V4 journeys and page objects in V4; do not reuse POM V1's page objects or `PomPages` wrapper, or page objects from another POM version.

| Location | Add or update this when... |
| --- | --- |
| `*Tests.cs` | Adding page-focused end-to-end tests. Group tests by their initial page object and route; use separate classes when either differs. |
| `PageObjectFactoryTests.cs` | Testing factory, binder, locator, scope, control-root, or readiness behavior. |
| `POM/` | Adding a page object or reusable application section used by a journey. |
| `Infrastructure/POM/` | Changing the factory, binder, locator resolution, or page-object base. |
| `Infrastructure/Locators/` | Changing locator attributes or supported selector strategies. |
| `Infrastructure/Controls/` | Adding or changing a reusable typed control. |
| `ShopDemo.Tests.E2E.Shared/` | Reusing common browser setup or test data; avoid duplicating it in V4. |

The current page-object map is:

- `ShopNavigation` owns a `NavigationBar` scoped to `.mud-appbar`.
- Workflow pages include `LoginPage`, `CatalogPage`, `InventoryPage`, `CartPage`, `CheckoutPage`, and `OrdersPage`.
- `ProductDetailsPage` owns tab activation and returns scoped `ProductDetailsOverviewTab` or `ProductDetailsInventoryTab` objects.

## Default approach

When adding a test, check the closest existing test class and page object, then inspect the application markup for stable selectors. For application tests, inherit from `ShopDemoPageTestV4<TPageObject>` and group tests in the same class only when they share an initial route and page-object type. The base navigates and binds `Model` before each test. Use the non-generic `ShopDemoPageTestV4` with explicit `CreatePageObject<T>()` calls for factory-focused tests or cases that need custom page-object creation. Expose stable UI elements through attributed page-object properties, prefer existing typed controls, and keep the test focused on user-visible behavior rather than selector or component implementation.

Use a stable `data-testid` as the default locator strategy. Use role or exact text locators when they express the UI target more clearly and reliably. Use CSS or XPath only when a stable test ID or suitable semantic locator is unavailable. Prefer existing controls such as `Button`, `TextField`, `Select`, `Tab`, `DataGrid`, and `DataGridPager` over repeating their low-level interactions.

## Add an end-to-end journey

1. Choose the initial page-object type and route, then add the test to a class derived from `ShopDemoPageTestV4<TPageObject>`.
2. Implement `PageUrl`; the base navigates there and binds `Model` before the test starts.
3. Use `E2ETestSettings` and `DemoAccount` for known test data.
4. Perform actions through `Model`, page-object methods, or typed controls, then assert on the resulting UI with Playwright assertions or xUnit.

For example, the existing catalog search journey follows the standard pattern:

```csharp
public sealed class CatalogPageTests : ShopDemoPageTestV4<CatalogPage>
{
    protected override string PageUrl => "/";

    [Fact]
    public async Task Catalog_can_be_searched_through_the_text_field_control()
    {
        await Model.ProductCards.First.WaitForAsync();

        await Model.SearchFor("Tote");

        await Expect(Model.GetProductCard(E2ETestSettings.CanvasToteId)).ToBeVisibleAsync();
        Assert.Equal("Tote", await Model.Search.GetValue());
        await Expect(Model.ProductCards).ToHaveCountAsync(1);
    }
}
```

Use `control.Expect()` for assertions on a typed control and `Expect(locator)` for an `ILocator`. Required properties are waited for during factory binding; `IsReady()` only checks current visibility and does not wait for a future state. Avoid fixed delays when a required locator or retrying Playwright assertion can express the wait.

## Add or update a page object

Page objects must have exactly one public constructor with the signature `(IPage page, ILocator? baseLocator = null)`. The factory constructs the object and binds its attributed properties before returning it.

Declare bound reference properties as `required` with an `init` setter. The factory assigns them during binding, avoiding `null!` while making the non-null member contract explicit. This C# `required` modifier is separate from `LocatorAttribute.Optional`, which controls whether the UI element must be visible during binding and readiness checks.

```csharp
public sealed class ExamplePage(IPage page, ILocator? baseLocator = null)
    : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "example-page")]
    public required ILocator Root { get; init; }

    [Locator(LocatorKind.Role, "button", Name = "Save")]
    public required Button Save { get; init; }
}
```

Every auto-bound public property must have exactly one `Locator` attribute, be an `ILocator` or `IBindableComponent` type (including a typed control), and have a setter. Properties without an attribute are ignored. Keep the property non-nullable: `Optional = true` means the UI element can be absent, not that the bound locator or control object is null.

| Locator declaration | Use |
| --- | --- |
| `[Locator(LocatorKind.DataTestId, "catalog-page")]` | Preferred for a stable application `data-testid`. |
| `[Locator(LocatorKind.Role, "button", Name = "Save")]` | Find by ARIA role and optionally exact accessible name. |
| `[Locator(LocatorKind.Text, "Inventory")]` | Find by exact text. |
| `[Locator(LocatorKind.Css, ".mud-appbar")]` | Use a stable CSS selector when a test ID or semantic locator is unsuitable. |
| `[Locator(LocatorKind.XPath, ".//button[@type='submit']")]` | Use XPath only when simpler stable strategies do not fit. |

Locators are required by default. `Optional = true` keeps the property bound but skips its initial visibility wait and does not add the property to its owning page object's readiness requirements. Use it for elements that are intentionally absent in some states, such as role-dependent navigation links—not to hide a missing required element. `Name` is only valid for role locators; role accessible-name and text matching are always exact.

Expose page-level operations as methods that describe the user action, and keep locator details inside the page object. Asynchronous methods omit the `Async` suffix, following the existing V4 API. Creating a page object does not navigate; methods should click or otherwise use the real UI when they represent a transition.

## Scope and page transitions

When `baseLocator` is null, attributes resolve from the `IPage`; otherwise they resolve within that locator. Nested page objects should receive the locator for their parent section so their properties cannot accidentally match elements elsewhere on the page.

Use the generic test base's `Model` as the initial page object; do not create the same initial object again. After that, use page-object methods for navigation and continue with returned objects instead of creating another object for the current route. Inside a page object, use its protected `CreatePageObject<T>(scope)` method to construct and bind a nested page object. For a tab transition, the parent page object should select the tab first and then create the tab object scoped to its panel. Do not switch tabs through application-specific test hooks. Do not construct attributed page objects directly with `new`; use the factory or `CreatePageObject<T>` so their properties are bound and required locators are waited for.

## Exceptional cases

### Selectors that depend on runtime data

Do not put a runtime product ID or other variable value in a fixed locator attribute. Keep the normal page scope stable, then build the dynamic locator and typed control inside the page-object operation. For example, `CatalogPage` scopes a product-specific button to its card:

```csharp
var card = Root.GetByTestId($"product-card-{productId}");
var addButton = new Button(Page, card.GetByTestId($"product-add-{productId}"));
await addButton.Click();
```

Keep this direct construction localized to the page object and scoped to the smallest stable parent. Use attributed properties for fixed selectors wherever possible.

### Controls whose matched element is not the interaction root

Normally, an attributed control's matched element is also the element to wait for and interact with. If a component such as MudBlazor `Select` places its test ID on a hidden internal input, keep the property attribute-bound and implement or extend a typed control that overrides `ResolveControlRoot`. The binder then waits for the visible root. Do not weaken the locator's required status or add an application-specific test hook to work around the markup.

### New binding or control behavior

Add focused tests to `PageObjectFactoryTests` when changing a binding rule, locator strategy, parent scoping, optional readiness, or control-root resolution. These tests use browser-rendered `SetContentAsync` markup. The live journeys primarily exercise test-ID attributes and the CSS app-bar locator; role, text, and XPath attribute resolution are covered by the focused tests.

## Validation

Run the solution build and E2E script:

```powershell
dotnet build ShopDemo.sln --no-restore
pwsh -NoLogo -NoProfile -File .\scripts\test-e2e.ps1
```
