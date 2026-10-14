using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Controls;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.Locators;
using ShopDemo.Tests.E2E.Pom.V4.Infrastructure.POM;

namespace ShopDemo.Tests.E2E.Pom.V4.POM;

public sealed class InventoryPage(IPage page, ILocator? baseLocator = null) : PageObject(page, baseLocator)
{
    [DataTestId("inventory-page")]
    public ILocator Root { get; private set; } = null!;

    public async Task UpdateStock(string productId, int stock)
    {
        // TODO: drawback: dynamic controls cannot be attribute-bound, so we have to create them directly. This is a trade-off between maintainability and flexibility.

        // The test ids contain the product id, so these controls cannot be attribute-bound; they are created directly.
        var item = Root.GetByTestId($"inventory-item-{productId}");
        var stockField = new NumericField(Page, item.GetByTestId($"inventory-stock-{productId}"));
        var saveButton = new Button(Page, item.GetByTestId($"inventory-save-{productId}"));

        await stockField.Fill(stock);
        await saveButton.Click();
        await item.GetByTestId($"inventory-current-stock-{productId}")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }
}
