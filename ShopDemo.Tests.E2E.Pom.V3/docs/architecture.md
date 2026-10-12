# ShopDemo.Tests.E2E.Pom.V3 — compile-time page objects

Same goals as V2 (ready-before-returned, required/optional UI, explicit scope/transitions), same
five journeys, zero reflection. Independent of V1/V2/Screenplay.

## How it works

- `PageComponent` base: tracks required locators/children, exposes `WaitReady()` / `IsReady()`.
- `Require(locator, description?)` / `Optional(locator)` — called once in the constructor, return
  `Required<ILocator>` / `Optional<ILocator>`. Type on the property tells you which it is.
- Using a wrapped locator needs `.Value` (`Email.Value.FillAsync(...)`).
- `PageComponent.Create<T>(page, baseLocator?)` constructs + awaits readiness. No reflection: `T`
  implements `IPageComponent<T>` (`static abstract CreateInstance(...)`) — compiler-checked CRTP
  instead of a runtime constructor scan.
- Nested-only components (`NavigationBar`, tab internals) skip `IPageComponent<T>`, use a plain
  `internal` ctor called by their parent.
- No attributes, no binder, no resolver — every locator is a plain Playwright call.

## Strengths

- No reflection — rename-safe, compiler-checked, step-through debuggable.
- Required/optional visible on the property line itself, no attribute vocabulary.
- Small framework (~90 lines) vs. V2's binder/resolver/attributes.
- Still gives named failures (`PageComponentNotReadyException`), via a hand-written description.

## Drawbacks

- `.Value` on *every* locator access, not just optional ones (required got wrapped too, for
  declaration-line symmetry).
- Required/optional registration must live in the constructor body — C# forbids calling instance
  methods from a property initializer (`CS0236`). Two places to look, not one.
- Scope is convention, not enforced: nothing stops a nested component from querying `page` instead
  of its own scope locator.
- Failure messages only as good as the description string a human remembered to pass.
- `CreateInstance` boilerplate repeated per page object instead of centralized in one factory.

## Vs. V2

| | V2 | V3 |
|---|---|---|
| Required/optional | attribute, `Required = false` | property type (`Required<T>`/`Optional<T>`) |
| Construction | reflection | compile-time (static-abstract interface) |
| Scope enforcement | structural (binder) | convention only |
| Call-site cost | none | `.Value` everywhere |
| Framework size | binder + resolver + attributes | base class + interface + 2 wrapper structs |
