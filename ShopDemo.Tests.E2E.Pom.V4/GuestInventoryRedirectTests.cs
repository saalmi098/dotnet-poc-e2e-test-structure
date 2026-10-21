using ShopDemo.Tests.E2E.Pom.V4.POM;
using Xunit;

namespace ShopDemo.Tests.E2E.Pom.V4;

public sealed class GuestInventoryRedirectTests : ShopDemoPageTestV4<LoginPage>
{
    protected override string PageUrl => "/manage/inventory";

    [Fact]
    public async Task Guest_is_redirected_to_login_from_inventory_management()
    {
        Assert.True(await Model.Form.IsVisibleAsync());
        Assert.False(await Page.GetByTestId("inventory-page").IsVisibleAsync());
    }
}
