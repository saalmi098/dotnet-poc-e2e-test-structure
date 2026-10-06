using Microsoft.Playwright;
using ShopDemo.E2E.Pom.Tests.Pages;

namespace ShopDemo.E2E.Pom.Tests;

public sealed class PomPagesFixture
{
    public PomPages Create(IPage page) => new(page);
}
