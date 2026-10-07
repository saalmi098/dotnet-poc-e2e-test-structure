**// TODO: document needs to be reviewed**

# Adding tests to ShopDemo POM V2

Use this guide when adding an E2E journey or the page objects it needs. For the broader design and diagrams, see [architecture.md](architecture.md).

## Project boundary and structure

POM V2 is an independent xUnit v3 project. Its project file references `ShopDemo.Tests.E2E.Shared` for `ShopDemoPageTest`, browser setup, test settings, and demo accounts. It does not reference POM V1. Keep new journeys and page objects in V2; do not use V1's `PomPages` wrapper.

| Location | Add or update this when... |
| --- | --- |
| `PomJourneysTests.cs` | Adding an end-to-end user journey. |
| `PageObjectFactoryTests.cs` | Testing factory, binder, selector, scope, or readiness behavior. |
| `POM/` | Adding a page object or reusable UI component used by a journey. |
| `Infrastructure/POM/` | Changing factory, binder, locator resolution, or the `PageObject` base. |
| `Infrastructure/Locators/` | Changing locator attributes or supported selector strategies. |
| `ShopDemo.Tests.E2E.Shared/` | Reusing common browser setup or test data; avoid duplicating it in V2. |

The existing page-object map is:

- `ShopNavigation` owns a `NavigationBar` component, scoped to `.mud-appbar`.
- Workflow pages include `LoginPage`, `CatalogPage`, `InventoryPage`, `CartPage`, `CheckoutPage`, and `OrdersPage`.
- `ProductDetailsPage` owns `ProductDetailsOverviewTab` and `ProductDetailsInventoryTab`.

## Add an end-to-end journey

1. Add a `[Fact]` to `PomJourneysTests` and inherit the shared `ShopDemoPageTest` setup.
2. Use the shared `Page` and `PageObjectFactory`; use `E2ETestSettings` or `DemoAccount` for known test data.
3. Navigate explicitly to the starting route in the test, or use a page-object transition that clicks a real UI control. Creating a page object does not navigate.
4. Assert through the returned page-object locators and methods. Call `IsReady()` when the journey needs to verify current required-field visibility.

For example:

```csharp
[Fact]
public async Task Shopper_can_view_product_details()
{
    await Page.GotoAsync("/");
    var factory = new PageObjectFactory();
    var catalog = await factory.Create<CatalogPage>(Page);
    var details = await catalog.OpenProductDetails(E2ETestSettings.CanvasToteId);
    var overview = await details.SwitchToOverviewTab();

    Assert.Equal("Market Canvas Tote", await overview.Name.InnerTextAsync());
    Assert.True(await overview.IsReady());
}
```

## Add or update a page object

Page objects must expose exactly one public constructor with this signature:

```csharp
public sealed class ExamplePage(IPage page, ILocator? baseLocator = null)
    : PageObject(page, baseLocator)
{
    [DataTestId("example-page")]
    public ILocator Root { get; private set; } = null!;
}
```

The factory constructs the object and asynchronously binds its public properties before it returns. Keep auto-bound locator properties non-nullable: `Required = false` means the UI element may be absent, not that the `ILocator` object is null. Setters may be private, as in the example.

Every auto-bound `ILocator` or nested `PageObject` property must have exactly one locator attribute. Properties without locator attributes are not auto-bound. Use the built-in selectors as follows:

| Attribute | Use |
| --- | --- |
| `[DataTestId("catalog-page")]` | Prefer a stable application `data-testid`. |
| `[Locator(LocatorKind.Role, "button", Name = "Save")]` | Find by ARIA role and, optionally, accessible name. |
| `[Locator(LocatorKind.Text, "Inventory", Exact = true)]` | Find by text; `Exact` controls exact matching. |
| `[Locator(LocatorKind.Css, ".mud-appbar")]` | Find by CSS selector. |
| `[Locator(LocatorKind.XPath, ".//button[@type='submit']")]` | Find by XPath expression. |

`DataTestId` and `Locator` are required by default. Binding waits for required locators to become visible and reports a `PageObjectBindingException` with the page-object type, property, and selector when binding fails. `Required = false` keeps the locator assigned but skips its initial visibility wait and excludes it from the parent's readiness requirements. A nested object's own `IsReady()` can still be checked separately.

`IsReady()` returns the current visibility result for the object's required locators and required children. It does not wait for a future state or perform navigation. Use page-object methods for actions and transitions, not `IsReady()`.

## Scope and transitions

When `baseLocator` is null, locator attributes resolve from the `IPage`. When a parent supplies a locator, the child's attributes resolve inside that component. For example, `ShopNavigation.Bar` is matched to the app bar, then its navigation links bind within the bar.

Keep component scope explicit:

- A page object performs actions on the locators it owns.
- A transition clicks its UI control, then calls `Create<T>(scope)` to return a bound object for the next view.
- Asynchronous page-object methods use names without an `Async` suffix, following the existing V2 API.

For Product Details, call `ProductDetailsPage.SwitchToOverviewTab()` or `SwitchToInventoryTab()`. The parent clicks the tab before creating the tab object with its panel as scope. Do not switch tabs through application-only test hooks. MudBlazor places the stable tab test IDs on label spans inside the actual tab controls; keep tab journeys clicking those labels and scoping fields to the corresponding panel.

## Focused factory tests and validation

`PageObjectFactoryTests` uses a small in-memory page to cover all locator strategies, parent scope, optional locators, readiness, binding errors, and the constructor convention. Add or update a focused test there when changing one of those framework rules; use `PomJourneysTests` for full application workflows.

Validate changes with:

```powershell
dotnet build ShopDemo.sln --no-restore
pwsh -NoLogo -NoProfile -File .\scripts\test-e2e.ps1
```
