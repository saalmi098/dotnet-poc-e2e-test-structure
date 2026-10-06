using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;

public sealed record RestockProduct(string ProductId, int Quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = actor.Ability<BrowseTheWeb>().Page;
        await page.GetByTestId("nav-inventory-desktop").ClickAsync();
        await page.GetByTestId("inventory-page").WaitForAsync();
        var item = page.GetByTestId($"inventory-item-{ProductId}");
        await item.GetByTestId($"inventory-stock-{ProductId}").Locator("input").FillAsync(Quantity.ToString());
        await item.GetByTestId($"inventory-save-{ProductId}").ClickAsync();
    }
}
