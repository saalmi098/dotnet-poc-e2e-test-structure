using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class LocatorResolverTests : ShopDemoPageTestV4
{
    [Fact]
    public async Task Factory_binds_all_locator_strategies_within_parent_scope()
    {
        await Page.SetContentAsync("""
            <button>Save</button><p>Text</p><div class="probe">Outside</div>
            <main data-testid="section">
              <button>Save</button><button>Save extra</button>
              <p>Text</p><p>Text extra</p>
              <div class="probe">Inside</div><span data-probe="xpath">XPath</span>
            </main>
            """);

        var model = await CreatePageObject<ProbePage>();

        Assert.True(await model.IsReady());
        await model.Section.Save.Expect().ToHaveTextAsync("Save");
        await Expect(model.Section.Text).ToHaveTextAsync("Text");
        await Expect(model.Section.Css).ToHaveTextAsync("Inside");
        await Expect(model.Section.XPath).ToHaveTextAsync("XPath");
        Assert.Equal(0, await model.Section.Optional.CountAsync());
        Assert.Null(model.Unbound);
    }

    [Fact]
    public async Task Factory_rejects_an_attributed_unsupported_property_type()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<InvalidPropertyPage>());

        Assert.Contains("InvalidPropertyPage.Text", exception.Message);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("empty")]
    [InlineData("role")]
    [InlineData("numeric-role")]
    [InlineData("kind")]
    public async Task Factory_rejects_invalid_locator_metadata(string scenario)
    {
        var factory = new PageObjectFactory();
        Task task = scenario switch
        {
            "name" => factory.Create<InvalidNamePage>(Page),
            "empty" => factory.Create<EmptySelectorPage>(Page),
            "role" => factory.Create<InvalidRolePage>(Page),
            "numeric-role" => factory.Create<NumericRolePage>(Page),
            _ => factory.Create<InvalidKindPage>(Page)
        };

        await Assert.ThrowsAsync<PageObjectBindingException>(async () => await task);
    }

    [Fact]
    public void GetBindingAttribute_rejects_attributes_on_unsupported_property_types()
    {
        var property = typeof(UnsupportedProperty).GetProperty(nameof(UnsupportedProperty.Value))
            ?? throw new InvalidOperationException("Test property was not found.");

        var exception = Assert.Throws<PageObjectBindingException>(
            () => LocatorResolver.GetBindingAttribute(typeof(UnsupportedProperty), property));

        Assert.Contains(nameof(UnsupportedProperty.Value), exception.Message);
        Assert.Contains(nameof(ILocator), exception.Message);
    }

    [Fact]
    public void ResolveLocator_rejects_undefined_aria_roles()
    {
        var attribute = new LocatorAttribute(LocatorKind.Role, "not-a-playwright-role");

        var exception = Assert.Throws<PageObjectBindingException>(
            () => LocatorResolver.ResolveLocator(Page, null, attribute));

        Assert.Contains("not a valid Playwright ARIA role", exception.Message);
    }

    private sealed class ProbePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "section")]
        public required ProbeSection Section { get; init; }

#pragma warning disable S3459 // Unassigned members should be removed
        public ILocator? Unbound { get; }
#pragma warning restore S3459 // Unassigned members should be removed
    }

    private sealed class ProbeSection(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "button", Name = "Save")]
        public required Button Save { get; init; }

        [Locator(LocatorKind.Text, "Text")]
        public required ILocator Text { get; init; }

        [Locator(LocatorKind.Css, ".probe")]
        public required ILocator Css { get; init; }

        [Locator(LocatorKind.XPath, ".//span[@data-probe='xpath']")]
        public required ILocator XPath { get; init; }

        [Locator(LocatorKind.DataTestId, "optional", Optional = true)]
        public required ILocator Optional { get; init; }
    }

    private sealed class InvalidPropertyPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Text, "text")]
        public required string Text { get; init; }
    }

    private sealed class InvalidNamePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Text, "text", Name = "name")]
        public required ILocator Text { get; init; }
    }

    private sealed class EmptySelectorPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Css, "")]
        public required ILocator Text { get; init; }
    }

    private sealed class InvalidRolePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "not-a-role")]
        public required ILocator Text { get; init; }
    }

    private sealed class NumericRolePage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "99999")]
        public required ILocator Text { get; init; }
    }

    private sealed class InvalidKindPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
    {
        [Locator((LocatorKind)99999, "unknown")]
        public required ILocator Text { get; init; }
    }

    private sealed class UnsupportedProperty
    {
        [Locator(LocatorKind.DataTestId, "unsupported")]
        public string Value => string.Empty;
    }
}
