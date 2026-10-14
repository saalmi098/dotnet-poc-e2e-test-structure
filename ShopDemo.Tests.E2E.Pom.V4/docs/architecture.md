# ShopDemo.Tests.E2E.Pom.V4 - typed controls on top of V2

V4 starts as an exact copy of POM V2 (see `../ShopDemo.Tests.E2E.Pom.V2/docs/architecture.md`) and adds typed control wrappers
(`Button`, `TextField`, `NumericField`, `Select`, ...) that encapsulate MudBlazor interaction behaviour instead of exposing raw `ILocator` properties.

Design decisions so far:

- Controls derive from a `Control` base class (sibling of `PageObject` under a shared `Component` base) and are bound by the same attribute-based binder.
- Controls expose a public `Locator` property and an `Expect()` method. They do **not** implement `ILocator`: Playwright's `Expect(ILocator)` casts to its internal `Locator` class, so any wrapper throws `InvalidCastException` (verified with a spike, Playwright 1.63.0, including a source-generated delegating wrapper).

- Binding is attribute-only: the binder binds a property only if it has `[DataTestId]` or `[Locator]`. Properties without an attribute are ignored and keep their default value (so a forgotten attribute surfaces as `null`, not as a binding error). This is what lets base classes expose helpers such as `Control.Locator` without special cases. A locator attribute on a property that is not an `ILocator`/`Component`, or more than one attribute on a property, still throws.

Status: step 1 (copy of V2, registered in the solution and `scripts/test-e2e.ps1`) is done. Control classes are not implemented yet.
