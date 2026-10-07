using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Screenplay.Abilities;

public sealed class BrowseTheWeb(IPage page) : IAbility
{
    public IPage Page { get; } = page;
}
