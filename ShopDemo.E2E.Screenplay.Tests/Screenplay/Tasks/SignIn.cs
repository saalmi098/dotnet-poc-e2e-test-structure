using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;
using ShopDemo.E2E.Shared;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;

public sealed record SignIn(DemoAccount Account) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        var loginLink = page.GetByTestId("nav-login-desktop");
        if (await loginLink.CountAsync() == 0)
        {
            await page.GotoAsync("/");
            await loginLink.WaitForAsync();
        }

        await loginLink.ClickAsync();
        await page.GetByTestId("login-email").FillAsync(Account.Email);
        await page.GetByTestId("login-password").FillAsync(Account.Password);
        await page.GetByTestId("login-submit").ClickAsync();
        await page.GetByTestId("nav-logout-desktop").WaitForAsync();
    }
}
