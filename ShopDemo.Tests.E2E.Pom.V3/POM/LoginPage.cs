using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V3.Infrastructure;
using ShopDemo.Tests.E2E.Shared;

namespace ShopDemo.Tests.E2E.Pom.V3.POM;

public sealed class LoginPage : PageComponent, IPageComponent<LoginPage>
{
    private readonly IPage _page;

    public Required<ILocator> Form { get; }
    public Required<ILocator> Email { get; }
    public Required<ILocator> Password { get; }
    public Required<ILocator> Submit { get; }

    private LoginPage(IPage page)
    {
        _page = page;
        Form = Require(page.GetByTestId("login-form"), $"{nameof(LoginPage)}.{nameof(Form)}");
        Email = Require(page.GetByTestId("login-email"), $"{nameof(LoginPage)}.{nameof(Email)}");
        Password = Require(page.GetByTestId("login-password"), $"{nameof(LoginPage)}.{nameof(Password)}");
        Submit = Require(page.GetByTestId("login-submit"), $"{nameof(LoginPage)}.{nameof(Submit)}");
    }

    static LoginPage IPageComponent<LoginPage>.CreateInstance(IPage page, ILocator? baseLocator) => new(page);

    public async Task<ShopNavigation> SignIn(DemoAccount account)
    {
        await Email.Value.FillAsync(account.Email);
        await Password.Value.FillAsync(account.Password);
        await Submit.Value.ClickAsync();

        var logoutLink = _page.GetByTestId("nav-logout-desktop");
        await logoutLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return await Create<ShopNavigation>(_page);
    }
}
