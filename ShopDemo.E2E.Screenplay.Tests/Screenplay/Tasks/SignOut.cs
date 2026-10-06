using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;

public sealed record SignOut : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-logout-desktop").ClickAsync();
        await page.GetByTestId("nav-login-desktop").WaitForAsync();
    }
}
