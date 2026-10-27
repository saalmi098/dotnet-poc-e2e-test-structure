using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class ComponentTreeBuilderTests : ShopDemoPageTestV4
{
    [Fact]
    public async Task Build_detects_recursive_component_metadata()
    {
        await Page.SetContentAsync("""<main data-testid="recursive-component"><span>Visible component</span></main>""");

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<RecursiveComponent>());

        Assert.Contains("Recursive component binding detected", exception.Message);
        Assert.Contains(nameof(RecursiveComponent), exception.Message);
    }

    [Fact]
    public async Task Build_detects_indirect_component_cycles()
    {
        await Page.SetContentAsync("<main></main>");

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<IndirectCyclePage>());

        Assert.Contains("Recursive component binding detected", exception.Message);
        Assert.Contains(nameof(IndirectCyclePage), exception.Message);
    }

    [Fact]
    public async Task Build_allows_same_component_type_in_separate_sibling_scopes()
    {
        await Page.SetContentAsync("""
            <main>
              <section data-testid="left"><span data-testid="value">Left</span></section>
              <section data-testid="right"><span data-testid="value">Right</span></section>
            </main>
            """);

        var component = await CreatePageObject<SiblingComponents>();

        Assert.NotSame(component.Left, component.Right);
        Assert.Equal("Left", await component.Left.Value.InnerTextAsync());
        Assert.Equal("Right", await component.Right.Value.InnerTextAsync());
    }

    [Fact]
    public async Task Optional_nested_component_does_not_wait_for_children_or_block_readiness()
    {
        await Page.SetContentAsync("<main></main>");

        var model = await CreatePageObject<OptionalPage>();

        Assert.True(await model.IsReady());
        Assert.False(await model.Section.IsReady());
        Assert.False(await model.Button.IsReady());
        Assert.Equal(0, await model.Section.Save.Locator.CountAsync());
    }

    [Fact]
    public async Task Optional_metadata_skips_subtree_waits_but_still_binds_properties()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(100);

        var model = await CreatePageObject<OptionalMetadataPage>();

        Assert.True(await model.IsReady());
        Assert.False(await model.Section.IsReady());
        Assert.False(await model.Button.IsReady());
        Assert.Equal(0, await model.Optional.CountAsync());
        Assert.Equal(0, await model.Section.Save.Locator.CountAsync());
    }

    [Fact]
    public async Task Explicit_optional_false_still_waits_for_required_locator()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(100);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<RequiredMetadataPage>());

        Assert.Contains(nameof(RequiredMetadataPage.Missing), exception.Message);
    }

    [Fact]
    public async Task Hidden_required_parent_fails_before_constructing_its_child()
    {
        await Page.SetContentAsync("""<section data-testid="section" hidden>Section</section>""");
        Page.SetDefaultTimeout(100);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<ParentWaitingPage>());

        Assert.Contains(nameof(ParentWaitingPage.Section), exception.Message);
        Assert.Contains("was not visible", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.IsNotType<ArgumentException>(exception.InnerException);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Required_component_child_on_control_is_rejected_before_child_construction(bool controlChild)
    {
        await Page.SetContentAsync("""<section data-testid="control">Control</section>""");
        Page.SetDefaultTimeout(100);
        var factory = new PageObjectFactory();

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(() => controlChild
            ? factory.Create<InvalidControlPage>(Page)
            : factory.Create<InvalidSectionControlPage>(Page));

        Assert.Contains(controlChild ? "InvalidParentControl.Child" : "InvalidSectionControl.Child", exception.Message);
        Assert.Contains("Controls are leaf components", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task Interface_only_component_binds_within_scope_and_tracks_readiness()
    {
        await Page.SetContentAsync("""
            <span data-testid="label">Outside</span>
            <section data-testid="component"><span data-testid="label">Inside</span></section>
            """);

        var model = await CreatePageObject<InterfaceComponentPage>();

        Assert.Same(Page, model.Content.Page);
        Assert.NotNull(model.Content.Scope);
        await Expect(model.Content.Label).ToHaveTextAsync("Inside");
        Assert.True(await model.IsReady());

        await model.Content.Label.EvaluateAsync("element => element.hidden = true");

        Assert.False(await model.Content.IsReady());
        Assert.False(await model.IsReady());
    }

    [Fact]
    public async Task Raw_locator_bound_on_control_contributes_to_readiness()
    {
        await Page.SetContentAsync("""<section data-testid="control"><span data-testid="label">Label</span></section>""");

        var model = await CreatePageObject<RawLocatorControlPage>();

        Assert.True(await model.IsReady());
        await model.Control.Label.EvaluateAsync("element => element.hidden = true");
        Assert.False(await model.Control.IsReady());
        Assert.False(await model.IsReady());
    }

    [Fact]
    public async Task Empty_scoped_page_readiness_includes_scope_visibility()
    {
        await Page.SetContentAsync("""<section data-testid="section">Section</section>""");
        var scope = Page.GetByTestId("section");
        var model = await new PageObjectFactory().Create<EmptyPage>(Page, scope);

        Assert.True(await model.IsReady());
        await scope.EvaluateAsync("element => element.hidden = true");
        Assert.False(await model.IsReady());
    }

    [Fact]
    public async Task Read_only_attributed_property_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<ReadOnlyPage>());

        Assert.Contains("setter", exception.Message);
    }

    [Fact]
    public async Task Build_rejects_indexer_properties_before_waiting_for_a_locator()
    {
        await Page.SetContentAsync("<main></main>");
        Page.SetDefaultTimeout(250);

        var exception = await Assert.ThrowsAsync<PageObjectBindingException>(
            () => CreatePageObject<IndexerProperty>());

        Assert.Contains(nameof(IndexerProperty), exception.Message);
        Assert.Contains("must not be an indexer", exception.Message);
    }

    [Fact]
    public async Task Page_object_readiness_includes_required_child_components()
    {
        await Page.SetContentAsync("""
            <main data-testid="required-child-root">
              <section data-testid="required-child-section">
                <button data-testid="required-child-save">Save</button>
              </section>
            </main>
            """);

        var page = await CreatePageObject<RequiredChildPage>();

        Assert.True(await page.IsReady());
        await page.Section.Save.EvaluateAsync("element => element.style.visibility = 'hidden'");
        Assert.False(await page.IsReady());
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
    public async Task Select_with_hidden_matched_input_waits_for_its_visible_root()
    {
        await Page.SetContentAsync("""
            <div class="mud-input-control">
              <div role="combobox" aria-expanded="false">Visible value</div>
              <input type="hidden" role="combobox" data-testid="select" value="value">
            </div>
            """);

        var model = await CreatePageObject<SelectPage>();

        Assert.True(await model.IsReady());
        await model.Select.Expect().ToContainTextAsync("Visible value");
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

    private sealed class RecursiveComponent(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "recursive-component")]
        public required RecursiveComponent Child { get; init; }
    }

    private sealed class SiblingComponents(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "left")]
        public required ScopedComponent Left { get; init; }

        [Locator(LocatorKind.DataTestId, "right")]
        public required ScopedComponent Right { get; init; }
    }

    private sealed class ScopedComponent(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "value")]
        public required ILocator Value { get; init; }
    }

    private sealed class IndexerProperty(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        private ILocator? _value;

        [Locator(LocatorKind.DataTestId, "missing")]
        public ILocator this[int index]
        {
            get => _value ?? throw new InvalidOperationException("The indexer has not been bound.");
            init => _value = value;
        }
    }

    private sealed class IndirectCyclePage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "section", Optional = true)]
        public required IndirectCycleSection Section { get; init; }
    }

    private sealed class IndirectCycleSection(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "page", Optional = true)]
        public required IndirectCyclePage Parent { get; init; }
    }

    private sealed class OptionalPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "section", Optional = true)]
        public required ProbeSection Section { get; init; }

        [Locator(LocatorKind.DataTestId, "button", Optional = true)]
        public required Button Button { get; init; }
    }

    private sealed class OptionalMetadataPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "section", Optional = true)]
        public required ProbeSection Section { get; init; }

        [Locator(LocatorKind.DataTestId, "button", Optional = true)]
        public required Button Button { get; init; }

        [Locator(LocatorKind.DataTestId, "optional", Optional = true)]
        public required ILocator Optional { get; init; }
    }

    private sealed class ProbeSection(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Role, "button", Name = "Save")]
        public required Button Save { get; init; }
    }

    private sealed class RequiredMetadataPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-element", Optional = false)]
        public required ILocator Missing { get; init; }
    }

    private sealed class ParentWaitingPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "section")]
        public required ThrowingPage Section { get; init; }
    }

    private sealed class ThrowingPage : PageObject
    {
#pragma warning disable S1144 // Unused private types or members should be removed
        public ThrowingPage(IPage page, ILocator? baseLocator = null) : base(page, baseLocator)
        {
            throw new ArgumentException("Constructor failure.");
        }
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private sealed class InvalidParentControl(IPage page, ILocator? baseLocator = null)
        : Control(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-child")]
        public required Button Child { get; init; }
    }

    private sealed class InvalidControlPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control")]
        public required InvalidParentControl Control { get; init; }
    }

    private sealed class InvalidSectionControl(IPage page, ILocator? baseLocator = null)
        : Control(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-child")]
        public required ThrowingPage Child { get; init; }
    }

    private sealed class InvalidSectionControlPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control")]
        public required InvalidSectionControl Control { get; init; }
    }

    private sealed class InterfaceComponent(IPage page, ILocator? baseLocator = null)
        : IBindableComponent
    {
        private readonly List<ILocator> _requiredLocators = [];

        public IPage Page { get; } = page;

        public ILocator? Scope { get; } = baseLocator;

        [Locator(LocatorKind.DataTestId, "label")]
        public required ILocator Label { get; init; }

        public void AddRequiredLocator(ILocator locator) => _requiredLocators.Add(locator);

        public async Task<bool> IsReady()
        {
            foreach (var locator in _requiredLocators)
            {
                if (!await locator.IsVisibleAsync())
                {
                    return false;
                }
            }

            return true;
        }
    }

    private sealed class InterfaceComponentPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "component")]
        public required InterfaceComponent Content { get; init; }
    }

    private sealed class RawLocatorControl(IPage page, ILocator? baseLocator = null)
        : Control(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "label")]
        public required ILocator Label { get; init; }
    }

    private sealed class RawLocatorControlPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control")]
        public required RawLocatorControl Control { get; init; }
    }

    private sealed class EmptyPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator);

    private sealed class ReadOnlyPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.Text, "text")]
        public ILocator Text => Page.GetByText("text");
    }

    private sealed class RequiredChildPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "required-child-root")]
        public required RequiredChildSection Section { get; init; }
    }

    private sealed class RequiredChildSection(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "required-child-save")]
        public required ILocator Save { get; init; }
    }

    private sealed class ProbeControl(IPage page, ILocator? baseLocator = null)
        : Control(page, baseLocator);

    private sealed class ControlProbePage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "control-probe")]
        public required ProbeControl Required { get; init; }

        [Locator(LocatorKind.DataTestId, "optional-control-probe", Optional = true)]
        public required ProbeControl Optional { get; init; }
    }

    private sealed class RootResolvingControl(IPage page, ILocator? baseLocator = null)
        : Control(page, baseLocator)
    {
        protected override ILocator ResolveControlRoot(ILocator matched)
            => matched.Locator("xpath=ancestor::div[@class='wrapper']");
    }

    private sealed class RootResolvingProbePage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "hidden-matched")]
        public required RootResolvingControl Field { get; init; }
    }

    private sealed class SelectPage(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "select")]
        public required Select Select { get; init; }
    }

    private sealed class UnattributedProbe(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public ILocator? Unbound { get; private set; }

        public ProbeControl? UnboundControl { get; private set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }

    private sealed class MissingRequiredProbe(IPage page, ILocator? baseLocator = null)
        : PageObject(page, baseLocator)
    {
        [Locator(LocatorKind.DataTestId, "missing-required-probe")]
        public required ILocator Missing { get; init; }
    }
}
