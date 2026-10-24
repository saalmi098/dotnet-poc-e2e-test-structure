using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Otel;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;

/// <summary>
/// Base class for leaf UI controls (button, text field, select, ...). A control wraps the element matched by its
/// locator attribute and encapsulates the interaction behaviour of that control.
/// </summary>
/// <remarks>
/// Deliberately does not implement <see cref="ILocator"/>: Playwright's <c>Expect(ILocator)</c> casts to its internal
/// locator class, so any wrapper would fail at runtime. Use <see cref="Expect"/> or <see cref="Locator"/> instead.
/// </remarks>
public abstract class Control(IPage page, ILocator? baseLocator = null) : IBindableComponent
{
    private readonly ComponentBindingContext _bindingContext = new(page, baseLocator);
    private ILocator? _root;

    /// <summary>
    /// The Playwright browser page used to interact with the application.
    /// </summary>
    protected IPage Page => _bindingContext.Page;

    /// <summary>
    /// The locator scope used to resolve this control's root.
    /// </summary>
    protected ILocator? BaseLocator => _bindingContext.BaseLocator;

    public void AddRequiredLocator(ILocator locator) => _bindingContext.AddRequiredLocator(locator);

    /// <summary>
    /// Reports whether this control's root and required locators are currently visible.
    /// </summary>
    public async Task<bool> IsReady() => await Locator.IsVisibleAsync() && await _bindingContext.IsReady();

    /// <summary>
    /// The locator of this control's root element. Escape hatch for Playwright APIs that need an <see cref="ILocator"/>.
    /// </summary>
    public ILocator Locator => _root ??= ResolveControlRoot(
        BaseLocator ?? throw new InvalidOperationException(
            $"Control '{GetType().Name}' has no locator; it must be bound as an attributed property of a page object."));

    /// <summary>
    /// Starts a Playwright assertion on this control's root element.
    /// </summary>
    public ILocatorAssertions Expect() => Assertions.Expect(Locator); // TODO: not necessarily needed. Instead of writing e.g. `await control.Expect().ToBeVisibleAsync()`, we could just write `await Expect(control.Locator).ToBeVisibleAsync()`.

    /// <summary>
    /// Resolves the element matched by the locator attribute to this control's interaction root.
    /// Override this when MudBlazor places the test ID on an internal element rather than the visible control.
    /// For example, a <c>MudSelect</c> puts its test ID on a hidden input, which cannot satisfy the binder's
    /// visibility wait; the select resolves that input to its visible control container instead.
    /// </summary>
    /// <remarks>
    /// The binder waits for the returned root and uses it for readiness checks and control interactions.
    /// Controls whose matched element is already the correct interaction root can use the default implementation.
    /// </remarks>
    protected virtual ILocator ResolveControlRoot(ILocator matched) => matched;

    /// <summary>
    /// Writes a trace line for an interaction with this control.
    /// </summary>
    protected void Trace(string message) => E2ETrace.Write(GetType(), message);

    /// <summary>
    /// Runs a user interaction (click, selection, typing, ...) inside an OpenTelemetry span named
    /// <c>"{ControlType}.{interactionName}"</c>, so the interaction is reported the same way a real
    /// frontend RUM integration would report it.
    /// </summary>
    /// <param name="interactionName">The interaction being performed, e.g. "Click", "SelectOption".</param>
    /// <param name="operation">The Playwright interaction to execute inside the span.</param>
    /// <param name="target">
    /// A human-readable identifier for what was interacted with (button label, tab text, option selected, ...),
    /// folded into the span name/tags so e.g. two different buttons don't both just show up as "Button.Click".
    /// Defaults to <see cref="GetLabel"/> when not given explicitly.
    /// </param>
    /// <param name="attributes">Additional span attributes, e.g. the option text selected.</param>
    protected async Task TraceInteraction(
        string interactionName,
        Func<Task> operation,
        string? target = null,
        IReadOnlyDictionary<string, object?>? attributes = null)
    {
        target ??= await TryGetLabel();
        await PageTracing.TraceInteractionAsync(Page, GetType().Name, interactionName, operation, target, attributes);
    }

    /// <summary>
    /// This control's own human-readable name, e.g. a button's caption or a tab's text. Used by
    /// <see cref="TraceInteraction"/> as the default span target when a call site doesn't pass one explicitly.
    /// Override for controls whose own visible text identifies them. Default is none: not every control has
    /// an obvious one (e.g. an input field's identifier is better expressed as the value passed to
    /// <see cref="TraceInteraction"/>).
    /// </summary>
    protected virtual Task<string?> GetLabel() => Task.FromResult<string?>(null);

    // Reading the label is a nice-to-have for the span name; a transient failure reading it must not
    // break the interaction itself.
    private async Task<string?> TryGetLabel()
    {
        try
        {
            return await GetLabel();
        }
        catch
        {
            return null;
        }
    }
}
