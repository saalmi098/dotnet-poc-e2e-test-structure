using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class LoginPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("login-form")]
    public ILocator Form { get; private set; } = null!;

    [DataTestId("login-email")]
    public TextField Email { get; private set; } = null!;

    [DataTestId("login-password")]
    public TextField Password { get; private set; } = null!;

    [DataTestId("login-submit")]
    public Button Submit { get; private set; } = null!;

    public async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Email.Fill(account.Email);
        await Password.Fill(account.Password);
        await Submit.Click();

        var logoutLink = Page.GetByTestId("nav-logout-desktop");
        await logoutLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await Create<ShopNavigation>();
    }
}
