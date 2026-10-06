using ShopDemo.E2E.Pom.Tests.Pages;
using ShopDemo.E2E.Shared;
using Xunit;

namespace ShopDemo.E2E.Pom.Tests;

public abstract class ShopDemoPomPageTest(PomPagesFixture pomPagesFixture)
    : ShopDemoPageTest, IClassFixture<PomPagesFixture>
{
    protected PomPages Pages { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Pages = pomPagesFixture.Create(Page);
    }
}
