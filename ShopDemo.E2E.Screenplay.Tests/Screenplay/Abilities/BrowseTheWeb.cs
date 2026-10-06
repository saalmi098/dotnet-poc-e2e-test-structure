using Microsoft.Playwright;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

public sealed class BrowseTheWeb(IPage page) : IAbility
{
    public IPage Page { get; } = page;
}
