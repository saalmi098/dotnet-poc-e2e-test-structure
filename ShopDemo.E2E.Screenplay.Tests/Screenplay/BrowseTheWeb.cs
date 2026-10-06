using Microsoft.Playwright;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay;

public sealed class BrowseTheWeb(IPage page) : IAbility
{
    public IPage Page { get; } = page;
}
