using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V1.Pages;

public sealed class LoginPage(IPage page)
{
    public async Task SignInAsync(DemoAccount account)
    {
        var loginLink = page.GetByTestId("nav-login-desktop");
        if (await loginLink.CountAsync() == 0)
        {
            await page.GotoAsync("/");
            await loginLink.WaitForAsync();
        }

        await loginLink.ClickAsync();
        await page.GetByTestId("login-email").FillAsync(account.Email);
        await page.GetByTestId("login-password").FillAsync(account.Password);
        await page.GetByTestId("login-submit").ClickAsync();
        await page.GetByTestId("nav-logout-desktop").WaitForAsync();
    }

    public async Task SignOutAsync()
    {
        await page.GetByTestId("nav-logout-desktop").ClickAsync();
        await page.GetByTestId("nav-login-desktop").WaitForAsync();
    }
}
