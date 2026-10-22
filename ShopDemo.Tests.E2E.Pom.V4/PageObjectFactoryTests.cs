using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class PageObjectFactoryTests : ShopDemoPageTestV4
{
    [Fact]
    public async Task Factory_rejects_a_null_page()
    {
        var factory = new PageObjectFactory();

#pragma warning disable CS8625
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => factory.Create<ScopedButtonProbe>(null));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task Factory_rejects_unsupported_constructor_conventions()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<UnsupportedConstructorProbe>());

        Assert.Contains(nameof(UnsupportedConstructorProbe), exception.Message);
        Assert.Contains("(IPage page, ILocator? baseLocator = null)", exception.Message);
    }

    [Fact]
    public async Task Factory_preserves_constructor_failure_as_inner_exception()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<ThrowingProbe>());

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public async Task Factory_can_be_reused_after_a_binding_failure()
    {
        Page.SetDefaultTimeout(100);
        var factory = new PageObjectFactory();
        await Assert.ThrowsAsync<PageObjectBindingException>(
            () => factory.Create<MissingRequiredProbe>(Page));
        await Page.SetContentAsync("""<main data-testid="missing-required-probe">Ready</main>""");

        var model = await factory.Create<MissingRequiredProbe>(Page);

        await Expect(model.Missing).ToHaveTextAsync("Ready");
        Assert.True(await model.IsReady());
    }

    [Fact]
    public async Task Factory_allows_concurrent_creations_without_false_cycle_detection()
    {
        await Page.SetContentAsync("""
            <section data-testid="first"><button data-testid="save">First</button></section>
            <section data-testid="second"><button data-testid="save">Second</button></section>
            """);
        var factory = new PageObjectFactory();

        var models = await Task.WhenAll(
            factory.Create<ScopedButtonProbe>(Page, Page.GetByTestId("first")),
            factory.Create<ScopedButtonProbe>(Page, Page.GetByTestId("second")));

        Assert.NotSame(models[0], models[1]);
        await models[0].Save.Expect().ToHaveTextAsync("First");
        await models[1].Save.Expect().ToHaveTextAsync("Second");
        Assert.True(await models[0].IsReady());
        Assert.True(await models[1].IsReady());
    }

    [Fact]
    public async Task Page_object_can_create_a_scoped_destination_after_transition()
    {
        await Page.SetContentAsync("""
            <button data-testid="save">Outside</button>
            <button data-testid="open" onclick="document.querySelector('[data-testid=section]').hidden = false">Open</button>
            <section data-testid="section" hidden><button data-testid="save">Inside</button></section>
            """);
        var model = await new PageObjectFactory().Create<TransitionProbe>(Page);

        var section = await model.OpenSection();

        await section.Save.Expect().ToHaveTextAsync("Inside");
        Assert.True(await section.IsReady());
    }

    private sealed class MissingRequiredProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-required-probe")]
        public required ILocator Missing { get; init; }
    }

#pragma warning disable CS9113 // Parameter is unread.
    private sealed class UnsupportedConstructorProbe(IPage page, string selector) : PageObject(page)
#pragma warning restore CS9113 // Parameter is unread.
    {
        [Locator(LocatorKind.DataTestId, "unsupported-constructor-probe")]
        public required ILocator Element { get; init; }
    }

    private sealed class ThrowingProbe : PageObject
    {
#pragma warning disable S1144 // Unused private types or members should be removed
        public ThrowingProbe(IPage page, ILocator? baseLocator = null) : base(page, baseLocator)
        {
            throw new ArgumentException("Constructor failure.");
        }
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private sealed class ScopedButtonProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "save")]
        public required Button Save { get; init; }
    }

    private sealed class TransitionProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "open")]
        public required Button Open { get; init; }

        [Locator(LocatorKind.DataTestId, "section", Optional = true)]
        public required ILocator Section { get; init; }

        public async Task<ScopedButtonProbe> OpenSection()
        {
            await Open.Click();
            return await CreatePageObject<ScopedButtonProbe>(Section);
        }
    }
}