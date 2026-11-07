namespace ShopDemo.Tests.E2E.Claude;

using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

// Claude Cowork
// Model: Sonnet 5.5, medium reasoning
// Agent has browser access to explore the live app
// Agent does not know the source code
// Zero-shot test(no example provided)
// Tests Test Flow 1 from Sciwo-Paper

// Test issues:
// - only one (recurring) issue: Cart-Link in Header was located by role(highlighted in red below), which resulted in multiple matches --> test failed.Fixing all of these links, by using the data-testid on the parent element, makes the whole test pass
// - Even the inteneded 5 seconds delay on the product details inventory tab got recognized, and a custom assertion delay(15s) got used

public class SciwoPaper_ClaudeTests
{
    private const string BaseUrl = "http://localhost:5178/";

    [Fact]
    public async Task Guest_Finds_Usb_Charger_Checks_Inventory_And_Cart_Survives_Reload()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        // Fresh context = guest with empty cart, no login and no prepared test data needed.
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        var page = await context.NewPageAsync();

        // 1. Open the catalog as a guest.
        await page.GotoAsync(BaseUrl);
        await Assertions.Expect(page).ToHaveTitleAsync(new Regex("Catalog"));
        await Assertions.Expect(page.GetByTestId("catalog-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("catalog-result-count")).ToHaveTextAsync("16 product(s) found");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Log in" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Cart (0)" })).ToBeVisibleAsync();

        // 2. Search for "USB".
        await page.GetByTestId("catalog-search").FillAsync("USB");
        await Assertions.Expect(page.GetByTestId("catalog-result-count")).ToHaveTextAsync("2 product(s) found");
        await Assertions.Expect(page.GetByTestId("product-card-usb-charger")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("product-card-mechanical-keyboard")).ToBeVisibleAsync();

        // 3. Price filter between 20 and 30 EUR.
        await page.GetByTestId("catalog-min-price").FillAsync("20");
        await page.GetByTestId("catalog-max-price").FillAsync("30");
        await Assertions.Expect(page.GetByTestId("catalog-result-count")).ToHaveTextAsync("1 product(s) found");
        await Assertions.Expect(page.GetByTestId("product-card-usb-charger")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("product-price-usb-charger")).ToHaveTextAsync("€29.90");
        await Assertions.Expect(page.GetByTestId("product-card-mechanical-keyboard")).ToHaveCountAsync(0);

        // 4. Open the product.
        await page.GetByTestId("product-details-usb-charger").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/products/usb-charger$"));
        await Assertions.Expect(page.GetByTestId("product-details")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("product-overview-name")).ToHaveTextAsync("65 W USB-C Charger");
        await Assertions.Expect(page.GetByTestId("product-overview-price")).ToHaveTextAsync("€29.90");
        await Assertions.Expect(page.GetByTestId("product-overview-stock")).ToContainTextAsync("30 available");

        // 5. Inventory tab shows the stock amount (loaded asynchronously, so rely on retrying assertions).
        await page.GetByTestId("product-inventory-tab").ClickAsync();
        await Assertions.Expect(page.GetByTestId("product-inventory-panel")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("product-inventory-grid")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("product-inventory-id")).ToHaveTextAsync("usb-charger");
        await Assertions.Expect(page.GetByTestId("product-inventory-stock")).ToHaveTextAsync("30");
        await Assertions.Expect(page.GetByTestId("product-inventory-availability")).ToHaveTextAsync("Available");

        // 6. Back to the overview and add the product to the cart.
        await page.GetByTestId("product-overview-tab").ClickAsync();
        await Assertions.Expect(page.GetByTestId("product-overview-panel")).ToBeVisibleAsync();
        await page.GetByTestId("product-details-add").ClickAsync();
        await Assertions.Expect(page.GetByText("Added to your cart.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Cart (1)" })).ToBeVisibleAsync();

        // 7. Cart page shows the item.
        await page.GetByRole(AriaRole.Link, new() { Name = "Cart (1)" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/cart$"));
        await Assertions.Expect(page.GetByTestId("cart-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("cart-item-usb-charger")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("cart-item-name-usb-charger")).ToHaveTextAsync("65 W USB-C Charger");
        await Assertions.Expect(page.GetByTestId("cart-line-total-usb-charger")).ToHaveTextAsync("€29.90");
        await Assertions.Expect(page.GetByTestId("cart-item-count")).ToHaveTextAsync("1 item(s)");
        await Assertions.Expect(page.GetByTestId("cart-subtotal")).ToHaveTextAsync("€29.90");

        // 8. Cart persists after a reload.
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("cart-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("cart-item-usb-charger")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("cart-item-name-usb-charger")).ToHaveTextAsync("65 W USB-C Charger");
        await Assertions.Expect(page.GetByTestId("cart-item-count")).ToHaveTextAsync("1 item(s)");
        await Assertions.Expect(page.GetByTestId("cart-subtotal")).ToHaveTextAsync("€29.90");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Cart (1)" })).ToBeVisibleAsync();
    }
}
