using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V3;

public sealed class PageComponentTests : ShopDemoPageTest
{
    [Fact]
    public async Task Required_and_optional_locators_are_distinct_in_readiness()
    {
        await Page.SetContentAsync("""
            <main data-testid="probe-parent">
              <button data-testid="probe-required">Required probe</button>
              <span data-testid="probe-optional" hidden>Optional probe</span>
            </main>
            """);

        var probe = await PageComponent.Create<LocatorProbe>(Page);

        Assert.True(await probe.IsReady());
        Assert.Equal("Required probe", await probe.Required.Value.InnerTextAsync());
        Assert.Equal(1, await probe.Hidden.Value.CountAsync());
    }

    [Fact]
    public async Task Missing_required_locator_fails_with_a_descriptive_error()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(250);

        var exception = await Assert.ThrowsAsync<PageComponentNotReadyException>(
            () => PageComponent.Create<MissingRequiredProbe>(Page));

        Assert.Contains(nameof(MissingRequiredProbe), exception.Message);
        Assert.Contains(nameof(MissingRequiredProbe.Missing), exception.Message);
    }

    [Fact]
    public async Task Nested_required_child_participates_in_parent_readiness()
    {
        await Page.SetContentAsync("""
            <main data-testid="probe-parent">
              <section data-testid="probe-child">
                <button data-testid="probe-required">Required probe</button>
              </section>
            </main>
            """);

        var probe = await PageComponent.Create<ParentProbe>(Page);

        Assert.True(await probe.IsReady());
        Assert.Equal("Required probe", await probe.Child.Required.Value.InnerTextAsync());
    }

    private sealed class LocatorProbe : PageComponent, IPageComponent<LocatorProbe>
    {
        public Required<ILocator> Required { get; }
        public Optional<ILocator> Hidden { get; }

        private LocatorProbe(IPage page)
        {
            Required = Require(page.GetByTestId("probe-required"), $"{nameof(LocatorProbe)}.{nameof(Required)}");
            Hidden = Optional(page.GetByTestId("probe-optional"));
        }

        static LocatorProbe IPageComponent<LocatorProbe>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);
    }

    private sealed class MissingRequiredProbe : PageComponent, IPageComponent<MissingRequiredProbe>
    {
        public Required<ILocator> Missing { get; }

        private MissingRequiredProbe(IPage page)
        {
            Missing = Require(page.GetByTestId("missing-required-probe"), $"{nameof(MissingRequiredProbe)}.{nameof(Missing)}");
        }

        static MissingRequiredProbe IPageComponent<MissingRequiredProbe>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);
    }

    private sealed class ChildProbe : PageComponent
    {
        public Required<ILocator> Required { get; }

        internal ChildProbe(ILocator scope)
        {
            Required = Require(scope.GetByTestId("probe-required"), $"{nameof(ChildProbe)}.{nameof(Required)}");
        }
    }

    private sealed class ParentProbe : PageComponent, IPageComponent<ParentProbe>
    {
        public ChildProbe Child { get; }

        private ParentProbe(IPage page)
        {
            Child = RequireChild(new ChildProbe(page.GetByTestId("probe-child")));
        }

        static ParentProbe IPageComponent<ParentProbe>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);
    }
}
