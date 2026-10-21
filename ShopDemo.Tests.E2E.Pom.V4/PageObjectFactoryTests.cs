using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class PageObjectFactoryTests : ShopDemoPageTestV4
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

        var probe = await CreatePageObject<LocatorProbe>();

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

        var probe = await CreatePageObject<LocatorProbe>();

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

        var probe = await CreatePageObject<ControlProbePage>();

        Assert.True(await probe.IsReady());
        await probe.Required.Expect().ToHaveTextAsync("Control probe");
        Assert.Equal("Control probe", await probe.Required.Locator.InnerTextAsync());
    }

    [Fact]
    public async Task Optional_control_does_not_block_readiness()
    {
        await Page.SetContentAsync("""<main data-testid="control-probe-parent"><span data-testid="control-probe">x</span></main>""");

        var probe = await CreatePageObject<ControlProbePage>();

        Assert.True(await probe.IsReady());
        Assert.Equal(0, await probe.Optional.Locator.CountAsync());
    }

    [Fact]
    public async Task Control_waits_for_its_resolved_root_not_for_the_matched_element()
    {
        await Page.SetContentAsync("""
            <div class="wrapper"><input type="hidden" data-testid="hidden-matched"><span>Visible root</span></div>
            """);

        var probe = await CreatePageObject<RootResolvingProbePage>();

        Assert.True(await probe.IsReady());
        await probe.Field.Expect().ToBeVisibleAsync();
        await probe.Field.Expect().ToContainTextAsync("Visible root");
    }

    [Fact]
    public async Task Properties_without_a_locator_attribute_are_not_bound()
    {
        await Page.SetContentAsync("<main></main>");

        var probe = await CreatePageObject<UnattributedProbe>();

        Assert.Null(probe.Unbound);
        Assert.Null(probe.UnboundControl);
    }

    [Fact]
    public async Task Missing_required_control_names_object_property_and_selector()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(250);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<ControlProbePage>());

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
            () => CreatePageObject<MissingRequiredProbe>());

        Assert.Contains(nameof(MissingRequiredProbe), exception.Message);
        Assert.Contains(nameof(MissingRequiredProbe.Missing), exception.Message);
        Assert.Contains("missing-required-probe", exception.Message);
    }

    [Fact]
    public async Task Locator_rejects_name_option_for_non_role_strategy()
    {
        await Page.SetContentAsync("<p>text probe</p>");

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<InvalidLocatorOptionsProbe>());

        Assert.Contains("Name option is only supported", exception.Message);
    }

    [Fact]
    public async Task Factory_rejects_unsupported_constructor_conventions()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<UnsupportedConstructorProbe>());

        Assert.Contains(nameof(UnsupportedConstructorProbe), exception.Message);
        Assert.Contains("(IPage page, ILocator? baseLocator = null)", exception.Message);
    }

    private sealed class LocatorProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "probe-parent")]
        public required LocatorProbeContent Content { get; init; }
    }

    private sealed class LocatorProbeContent(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "button", Name = "Save probe")]
        public required ILocator Button { get; init; }

        [Locator(LocatorKind.Text, "Text probe")]
        public required ILocator Text { get; init; }

        [Locator(LocatorKind.Css, ".css-probe")]
        public required ILocator Css { get; init; }

        [Locator(LocatorKind.XPath, ".//span[@data-probe='xpath']")]
        public required ILocator XPath { get; init; }

        [Locator(LocatorKind.DataTestId, "optional-probe", Optional = true)]
        public required ILocator Optional { get; init; }
    }

    private sealed class ProbeControl(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator);

    private sealed class ControlProbePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control-probe")]
        public required ProbeControl Required { get; init; }

        [Locator(LocatorKind.DataTestId, "optional-control-probe", Optional = true)]
        public required ProbeControl Optional { get; init; }
    }

    private sealed class RootResolvingControl(IPage page, ILocator? baseLocator = null) : Control(page, baseLocator)
    {
        protected override ILocator ResolveControlRoot(ILocator matched) => matched.Locator("xpath=ancestor::div[@class='wrapper']");
    }

    private sealed class RootResolvingProbePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "hidden-matched")]
        public required RootResolvingControl Field { get; init; }
    }

    private sealed class UnattributedProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public ILocator? Unbound { get; private set; }

        public ProbeControl? UnboundControl { get; private set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }

    private sealed class MissingRequiredProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-required-probe")]
        public required ILocator Missing { get; init; }
    }

    private sealed class InvalidLocatorOptionsProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Text, "text probe", Name = "not a role")]
        public required ILocator Invalid { get; init; }
    }

#pragma warning disable CS9113 // Parameter is unread.
    private sealed class UnsupportedConstructorProbe(IPage page, string selector) : PageObject(page)
#pragma warning restore CS9113 // Parameter is unread.
    {
        [Locator(LocatorKind.DataTestId, "unsupported-constructor-probe")]
        public required ILocator Element { get; init; }
    }
}