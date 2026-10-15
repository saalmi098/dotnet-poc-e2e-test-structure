using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class PageObjectFactoryTests : ShopDemoPageTest
{
    [Fact]
    public async Task Binds_each_locator_strategy_scoped_to_its_parent()
    {
        await Page.SetContentAsync("""
            <main data-testid="probe-parent">
              <button>Save probe</button>
              <button>Save probe extra</button>
              <p>Text probe</p>
              <p>Text probe extra</p>
              <div class="css-probe">CSS probe</div>
              <span data-probe="xpath">XPath probe</span>
              <span data-testid="optional-probe" hidden>Optional probe</span>
            </main>
            """);

        var probe = await new PageObjectFactory().Create<LocatorProbe>(Page);

        Assert.True(await probe.IsReady());
        Assert.Equal("Save probe", await probe.Content.Button.InnerTextAsync());
        Assert.Equal("Text probe", await probe.Content.Text.InnerTextAsync());
        Assert.Equal("CSS probe", await probe.Content.Css.InnerTextAsync());
        Assert.Equal("XPath probe", await probe.Content.XPath.InnerTextAsync());
        Assert.Equal(1, await probe.Content.Optional.CountAsync());
    }

    [Fact]
    public async Task Page_object_readiness_includes_required_child_components()
    {
        await Page.SetContentAsync("""
            <main data-testid="probe-parent">
              <button>Save probe</button>
              <p>Text probe</p>
              <div class="css-probe">CSS probe</div>
              <span data-probe="xpath">XPath probe</span>
              <span data-testid="optional-probe" hidden>Optional probe</span>
            </main>
            """);

        var probe = await new PageObjectFactory().Create<LocatorProbe>(Page);

        Assert.True(await probe.IsReady());
        await probe.Content.Button.EvaluateAsync("element => element.style.visibility = 'hidden'");
        Assert.False(await probe.IsReady());
    }

    [Fact]
    public async Task Binds_control_properties_with_the_matched_locator_as_root()
    {
        await Page.SetContentAsync("""
            <main data-testid="control-probe-parent">
              <span data-testid="control-probe">Control probe</span>
              <span data-testid="optional-control-probe" hidden>Optional control</span>
            </main>
            """);

        var probe = await new PageObjectFactory().Create<ControlProbePage>(Page);

        Assert.True(await probe.IsReady());
        await probe.Required.Expect().ToHaveTextAsync("Control probe");
        Assert.Equal("Control probe", await probe.Required.Locator.InnerTextAsync());
    }

    [Fact]
    public async Task Optional_control_does_not_block_readiness()
    {
        await Page.SetContentAsync("""<main data-testid="control-probe-parent"><span data-testid="control-probe">x</span></main>""");

        var probe = await new PageObjectFactory().Create<ControlProbePage>(Page);

        Assert.True(await probe.IsReady());
        Assert.Equal(0, await probe.Optional.Locator.CountAsync());
    }

    [Fact]
    public async Task Control_waits_for_its_resolved_root_not_for_the_matched_element()
    {
        await Page.SetContentAsync("""
            <div class="wrapper"><input type="hidden" data-testid="hidden-matched"><span>Visible root</span></div>
            """);

        var probe = await new PageObjectFactory().Create<RootResolvingProbePage>(Page);

        Assert.True(await probe.IsReady());
        await probe.Field.Expect().ToBeVisibleAsync();
        await probe.Field.Expect().ToContainTextAsync("Visible root");
    }

    [Fact]
    public async Task Properties_without_a_locator_attribute_are_not_bound()
    {
        await Page.SetContentAsync("<main></main>");

        var probe = await new PageObjectFactory().Create<UnattributedProbe>(Page);

        Assert.Null(probe.Unbound);
        Assert.Null(probe.UnboundControl);
    }

    [Fact]
    public async Task Missing_required_control_names_object_property_and_selector()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(250);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => new PageObjectFactory().Create<ControlProbePage>(Page));

        Assert.Contains(nameof(ControlProbePage), exception.Message);
        Assert.Contains(nameof(ControlProbePage.Required), exception.Message);
        Assert.Contains("control-probe", exception.Message);
    }

    [Fact]
    public async Task Required_binding_failure_names_object_property_and_selector()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(250);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => new PageObjectFactory().Create<MissingRequiredProbe>(Page));

        Assert.Contains(nameof(MissingRequiredProbe), exception.Message);
        Assert.Contains(nameof(MissingRequiredProbe.Missing), exception.Message);
        Assert.Contains("missing-required-probe", exception.Message);
    }

    [Fact]
    public async Task Locator_rejects_name_option_for_non_role_strategy()
    {
        await Page.SetContentAsync("<p>text probe</p>");

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => new PageObjectFactory().Create<InvalidLocatorOptionsProbe>(Page));

        Assert.Contains("Name option is only supported", exception.Message);
    }

    [Fact]
    public async Task Factory_rejects_unsupported_constructor_conventions()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => new PageObjectFactory().Create<UnsupportedConstructorProbe>(Page));

        Assert.Contains(nameof(UnsupportedConstructorProbe), exception.Message);
        Assert.Contains("(IPage page, ILocator? baseLocator = null)", exception.Message);
    }

    private sealed class LocatorProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "probe-parent")]
        public LocatorProbeContent Content { get; private set; } = null!;
    }

    private sealed class LocatorProbeContent(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "button", Name = "Save probe")]
        public ILocator Button { get; private set; } = null!;

        [Locator(LocatorKind.Text, "Text probe")]
        public ILocator Text { get; private set; } = null!;

        [Locator(LocatorKind.Css, ".css-probe")]
        public ILocator Css { get; private set; } = null!;

        [Locator(LocatorKind.XPath, ".//span[@data-probe='xpath']")]
        public ILocator XPath { get; private set; } = null!;

        [Locator(LocatorKind.DataTestId, "optional-probe", Required = false)]
        public ILocator Optional { get; private set; } = null!;
    }

    private sealed class ProbeControl(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator);

    private sealed class ControlProbePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control-probe")]
        public ProbeControl Required { get; private set; } = null!;

        [Locator(LocatorKind.DataTestId, "optional-control-probe", Required = false)]
        public ProbeControl Optional { get; private set; } = null!;
    }

    private sealed class RootResolvingControl(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
    {
        protected override ILocator ResolveControlRoot(ILocator matched) => matched.Locator("xpath=ancestor::div[@class='wrapper']");
    }

    private sealed class RootResolvingProbePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "hidden-matched")]
        public RootResolvingControl Field { get; private set; } = null!;
    }

    private sealed class UnattributedProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        public ILocator? Unbound { get; private set; }

        public ProbeControl? UnboundControl { get; private set; }
    }

    private sealed class MissingRequiredProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-required-probe")]
        public ILocator Missing { get; private set; } = null!;
    }

    private sealed class InvalidLocatorOptionsProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Text, "text probe", Name = "not a role")]
        public ILocator Invalid { get; private set; } = null!;
    }

    private sealed class UnsupportedConstructorProbe(IPage page, string selector) : PageObject(page)
    {
        [Locator(LocatorKind.DataTestId, "unsupported-constructor-probe")]
        public ILocator Element { get; private set; } = null!;
    }
}