# POM Page Object Factory Proposal

## Problem

- The current `PomPages` creates every page-object wrapper for each test.
- Most tests use only a few of them.
- Adding a page also means changing the central `PomPages` class.

## Proposed approach

- Use a per-test `PageObjectFactory` bound to Playwright's `IPage`.
- `GetPage<T>()` creates a page object on first use and caches it.
- Keep the factory and its page objects scoped to one test. Do not share them through a class fixture.
- This example assumes each page object has a public constructor that accepts `IPage`.

```csharp
public sealed class PageObjectFactory(IPage page)
{
    private readonly Dictionary<Type, object> _cache = [];

    public TPage GetPage<TPage>() where TPage : class
    {
        if (_cache.TryGetValue(typeof(TPage), out var cached))
            return (TPage)cached;

        var created = Activator.CreateInstance(typeof(TPage), page) as TPage
            ?? throw new InvalidOperationException(
                $"{typeof(TPage).Name} needs a public constructor accepting IPage.");

        _cache.Add(typeof(TPage), created);
        return created;
    }
}
```

Tests request only the page objects they use:

```csharp
await Pages.GetPage<LoginPage>().SignInAsync(manager);
await Pages.GetPage<InventoryPage>().UpdateStockAsync(productId, 4);
```

`Pages` is created once per test using that test's `PageTest.Page`.

## Trade-off

- This avoids a hardcoded list of page objects.
- Reflection means a missing or changed constructor fails at runtime.
- If page objects need more dependencies, consider a typed factory or .NET dependency injection instead.

This is a proposal. The current POM suite still uses `PomPages`.

## References

- [Playwright .NET Page Object Model](https://playwright.dev/dotnet/docs/pom)
- [Playwright .NET test runners](https://playwright.dev/dotnet/docs/test-runners)
