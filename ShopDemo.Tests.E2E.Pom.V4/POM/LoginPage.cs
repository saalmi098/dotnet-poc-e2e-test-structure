using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class LoginPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [Locator(LocatorKind.DataTestId, "login-form")]
    public required ILocator Form { get; init; }

    [Locator(LocatorKind.DataTestId, "login-email")]
    public required TextField Email { get; init; }

    [Locator(LocatorKind.DataTestId, "login-password")]
    public required TextField Password { get; init; }

    [Locator(LocatorKind.DataTestId, "login-submit")]
    public required Button Submit { get; init; }

    public async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Email.Fill(account.Email);
        await Password.Fill(account.Password);
        await Submit.Click();

        var logoutLink = Page.GetByTestId("nav-logout-desktop");
        await logoutLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await CreatePageObject<ShopNavigation>();
    }
}
