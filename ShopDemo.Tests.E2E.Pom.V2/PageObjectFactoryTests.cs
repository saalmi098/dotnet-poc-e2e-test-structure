using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V2;

public sealed class PageObjectFactoryTests : ShopDemoPageTest
{
    [Fact]
    public async Task Binds_each_locator_strategy_scoped_to_its_parent()
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
        Assert.Equal("Save probe", await probe.Content.Button.InnerTextAsync());
        Assert.Equal("Text probe", await probe.Content.Text.InnerTextAsync());
        Assert.Equal("CSS probe", await probe.Content.Css.InnerTextAsync());
        Assert.Equal("XPath probe", await probe.Content.XPath.InnerTextAsync());
        Assert.Equal(1, await probe.Content.Optional.CountAsync());
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
    public async Task Factory_rejects_unsupported_constructor_conventions()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => new PageObjectFactory().Create<UnsupportedConstructorProbe>(Page));

        Assert.Contains(nameof(UnsupportedConstructorProbe), exception.Message);
        Assert.Contains("(IPage page, ILocator? baseLocator = null)", exception.Message);
    }

    private sealed class LocatorProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [DataTestId("probe-parent")]
        public LocatorProbeContent Content { get; private set; } = null!;
    }

    private sealed class LocatorProbeContent(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "button", Name = "Save probe")]
        public ILocator Button { get; private set; } = null!;

        [Locator(LocatorKind.Text, "Text probe", Exact = true)]
        public ILocator Text { get; private set; } = null!;

        [Locator(LocatorKind.Css, ".css-probe")]
        public ILocator Css { get; private set; } = null!;

        [Locator(LocatorKind.XPath, ".//span[@data-probe='xpath']")]
        public ILocator XPath { get; private set; } = null!;

        [DataTestId("optional-probe", Required = false)]
        public ILocator Optional { get; private set; } = null!;
    }

    private sealed class MissingRequiredProbe(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [DataTestId("missing-required-probe")]
        public ILocator Missing { get; private set; } = null!;
    }

    private sealed class UnsupportedConstructorProbe(IPage page, string selector) : PageObject(page)
    {
        [DataTestId("unsupported-constructor-probe")]
        public ILocator Element { get; private set; } = null!;
    }
}