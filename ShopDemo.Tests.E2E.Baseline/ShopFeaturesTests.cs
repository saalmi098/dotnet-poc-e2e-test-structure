using Microsoft.Playwright;
using ShopDemo.Tests.E2E.Shared;
using Xunit;

namespace ShopDemo.Tests.E2E.Baseline;

// Direct Playwright tests for the webshop features beyond the original four journeys.
public sealed class ShopFeaturesTests : ShopDemoPageTest
{
    [Fact]
    public async Task Visitor_can_register_log_out_and_log_in_again()
    {
        var email = $"new.shopper.{Guid.NewGuid():N}@example.com";
        await Page.GotoAsync("/register");

        await Page.GetByTestId("register-name").FillAsync("New Shopper");
        await Page.GetByTestId("register-email").FillAsync(email);
        await Page.GetByTestId("register-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-confirm-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-submit").ClickAsync();

        await Expect(Page.GetByTestId("nav-account-desktop")).ToHaveTextAsync("New Shopper");
        await Page.GetByTestId("nav-logout-desktop").ClickAsync();
        await Expect(Page.GetByTestId("nav-login-desktop")).ToBeVisibleAsync();

        await LoginAsync(email, "secret-pass-1");
        await Expect(Page.GetByTestId("nav-account-desktop")).ToHaveTextAsync("New Shopper");
    }

    [Fact]
    public async Task Registration_rejects_an_existing_email()
    {
        await Page.GotoAsync("/register");

        await Page.GetByTestId("register-name").FillAsync("Duplicate");
        await Page.GetByTestId("register-email").FillAsync(E2ETestSettings.Shopper.Email);
        await Page.GetByTestId("register-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-confirm-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-submit").ClickAsync();

        await Expect(Page.GetByTestId("register-error")).ToContainTextAsync("already exists");
    }

    [Fact]
    public async Task Login_with_a_wrong_password_shows_an_error()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, "wrong-password");

        await Expect(Page.GetByTestId("login-error")).ToContainTextAsync("incorrect");
        await Expect(Page.GetByTestId("nav-logout-desktop")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Catalog_can_be_sorted_filtered_and_paged()
    {
        await Page.GotoAsync("/");
        await Expect(Page.GetByTestId("catalog-result-count")).ToHaveTextAsync("16 product(s) found");
        await Expect(Page.Locator("[data-testid^='product-card-']")).ToHaveCountAsync(12);

        await Page.GetByTestId("catalog-pagination").GetByRole(AriaRole.Button, new() { Name = "Page 2" }).ClickAsync();
        await Expect(Page.Locator("[data-testid^='product-card-']")).ToHaveCountAsync(4);

        await Page.GetByTestId("catalog-in-stock").ClickAsync();
        await Expect(Page.GetByTestId("catalog-result-count")).ToHaveTextAsync("14 product(s) found");
        await Expect(Page.GetByTestId("product-card-canvas-tote")).ToHaveCountAsync(0);

        await SelectOptionAsync("catalog-sort", "Price: low to high");
        await Expect(Page.Locator("[data-testid^='product-card-']").First).ToHaveAttributeAsync("data-testid", "product-card-pencil-set");

        await Page.GetByTestId("catalog-clear-filters").ClickAsync();
        await Expect(Page.GetByTestId("catalog-result-count")).ToHaveTextAsync("16 product(s) found");
    }

    [Fact]
    public async Task Catalog_can_be_limited_to_sale_items()
    {
        await Page.GotoAsync("/");

        await Page.GetByTestId("catalog-on-sale").ClickAsync();

        await Expect(Page.GetByTestId("catalog-result-count")).ToHaveTextAsync("3 product(s) found");
        await Expect(Page.GetByTestId("product-sale-badge-wool-blanket")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("product-original-price-wool-blanket")).ToContainTextAsync("119.00");
    }

    [Fact]
    public async Task Guest_cart_survives_login_and_a_coupon_reduces_the_total()
    {
        await Page.GotoAsync("/");
        await Page.GetByTestId("product-add-desk-lamp").ClickAsync();
        await Expect(Page.GetByTestId("nav-cart-desktop")).ToHaveTextAsync("Cart (1)");

        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Expect(Page.GetByTestId("nav-cart-desktop")).ToHaveTextAsync("Cart (1)");

        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Expect(Page.GetByTestId("cart-subtotal")).ToHaveTextAsync("€46.00");
        await Expect(Page.GetByTestId("cart-shipping")).ToHaveTextAsync("€4.90");

        await Page.GetByTestId("cart-coupon-input").FillAsync("welcome10");
        await Page.GetByTestId("cart-coupon-apply").ClickAsync();

        await Expect(Page.GetByTestId("cart-coupon-code")).ToHaveTextAsync("WELCOME10");
        await Expect(Page.GetByTestId("cart-discount")).ToHaveTextAsync("-€4.60");
        await Expect(Page.GetByTestId("cart-total")).ToHaveTextAsync("€46.30");
    }

    [Fact]
    public async Task Invalid_coupons_and_quantity_limits_are_reported()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/");
        await Page.GetByTestId("product-add-pencil-set").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();

        await Page.GetByTestId("cart-coupon-input").FillAsync("OLD20");
        await Page.GetByTestId("cart-coupon-apply").ClickAsync();
        await Expect(Page.GetByTestId("cart-coupon-error")).ToContainTextAsync("expired");

        await Page.GetByTestId("cart-coupon-input").FillAsync("SAVE5");
        await Page.GetByTestId("cart-coupon-apply").ClickAsync();
        await Expect(Page.GetByTestId("cart-coupon-error")).ToContainTextAsync("minimum");

        await Page.GetByTestId("cart-quantity-pencil-set").Locator("input").FillAsync("3");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.GetByTestId("cart-line-total-pencil-set")).ToHaveTextAsync("€24.00");
        await Expect(Page.GetByTestId("cart-free-shipping-hint")).ToContainTextAsync("€26.00");
    }

    [Fact]
    public async Task Declined_card_is_reported_and_an_invoice_order_can_be_placed_and_cancelled()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/");
        await Page.GetByTestId("product-add-ceramic-mug").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();

        await Page.GetByTestId("checkout-payment-card").ClickAsync();
        await Page.GetByTestId("checkout-card-holder").FillAsync("Demo Shopper");
        await Page.GetByTestId("checkout-card-number").FillAsync("4000 0000 0000 0002");
        await Page.GetByTestId("checkout-card-expiry").FillAsync("12/40");
        await Page.GetByTestId("checkout-card-cvc").FillAsync("123");
        await Page.GetByTestId("checkout-confirm").ClickAsync();
        await Expect(Page.GetByTestId("checkout-error")).ToContainTextAsync("declined");

        await Page.GetByTestId("checkout-payment-invoice").ClickAsync();
        await Page.GetByTestId("checkout-shipping-express").ClickAsync();
        await Expect(Page.GetByTestId("checkout-shipping-cost")).ToHaveTextAsync("€12.90");
        await Expect(Page.GetByTestId("checkout-total")).ToHaveTextAsync("€36.90");
        await Page.GetByTestId("checkout-confirm").ClickAsync();

        var orderNumber = await Page.GetByTestId("checkout-order-number").InnerTextAsync();
        await Page.GetByTestId("checkout-view-order").ClickAsync();
        await Expect(Page.GetByTestId("order-details-status")).ToHaveTextAsync("Pending");
        await Expect(Page.GetByTestId("order-details-total")).ToHaveTextAsync("€36.90");

        await Page.GetByTestId("order-cancel").ClickAsync();
        await Page.GetByTestId("order-cancel-confirm").ClickAsync();
        await Expect(Page.GetByTestId("order-details-status")).ToHaveTextAsync("Cancelled");
        await Expect(Page.GetByTestId("order-timeline-Cancelled")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("order-cancel")).ToHaveCountAsync(0);

        await Page.GotoAsync("/");
        await Expect(Page.GetByTestId("product-stock-ceramic-mug")).ToHaveTextAsync("12 in stock");
        Assert.False(string.IsNullOrEmpty(orderNumber));
    }

    [Fact]
    public async Task Placing_an_order_reduces_the_stock_shown_in_the_catalog()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/");
        await Expect(Page.GetByTestId("product-stock-desk-lamp")).ToHaveTextAsync("6 in stock");
        await Page.GetByTestId("product-add-desk-lamp").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();
        await Page.GetByTestId("checkout-confirm").ClickAsync();
        await Expect(Page.GetByTestId("checkout-success")).ToBeVisibleAsync();

        await Page.GetByTestId("nav-catalog-desktop").ClickAsync();

        await Expect(Page.GetByTestId("product-stock-desk-lamp")).ToHaveTextAsync("5 in stock");
    }

    [Fact]
    public async Task Checkout_requires_an_address()
    {
        var email = $"no.address.{Guid.NewGuid():N}@example.com";
        await Page.GotoAsync("/register");
        await Page.GetByTestId("register-name").FillAsync("No Address");
        await Page.GetByTestId("register-email").FillAsync(email);
        await Page.GetByTestId("register-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-confirm-password").FillAsync("secret-pass-1");
        await Page.GetByTestId("register-submit").ClickAsync();
        await Expect(Page.GetByTestId("nav-logout-desktop")).ToBeVisibleAsync();
        await Page.GotoAsync("/");
        await Page.GetByTestId("product-add-ceramic-mug").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();

        await Page.GetByTestId("checkout-confirm").ClickAsync();

        await Expect(Page.GetByTestId("checkout-success")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("checkout-form")).ToContainTextAsync("Enter a street address.");
    }

    [Fact]
    public async Task Shopper_can_manage_a_wishlist()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/");

        await Page.GetByTestId("product-wishlist-steel-bottle").ClickAsync();
        await Page.GetByTestId("nav-wishlist-desktop").ClickAsync();

        await Expect(Page.GetByTestId("wishlist-item-steel-bottle")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("wishlist-item-desk-lamp")).ToBeVisibleAsync();

        await Page.GetByTestId("wishlist-add-to-cart-steel-bottle").ClickAsync();
        await Expect(Page.GetByTestId("nav-cart-desktop")).ToHaveTextAsync("Cart (1)");
        await Page.GetByTestId("wishlist-remove-desk-lamp").ClickAsync();
        await Expect(Page.GetByTestId("wishlist-item-desk-lamp")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Shopper_can_review_a_product_once_and_the_rating_updates()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/products/steel-bottle");
        await Page.GetByTestId("product-reviews-tab").ClickAsync();
        await Expect(Page.GetByTestId("product-reviews-count-4")).ToHaveTextAsync("1");

        await Page.GetByTestId("review-rating").Locator(".mud-rating-item").Nth(1).ClickAsync();
        await Page.GetByTestId("review-title").FillAsync("Decent bottle");
        await Page.GetByTestId("review-comment").FillAsync("Keeps drinks cold but the lid is stiff.");
        await Page.GetByTestId("review-submit").ClickAsync();

        await Expect(Page.GetByTestId("review-already-reviewed")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Decent bottle")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("product-reviews-count-2")).ToHaveTextAsync("1");
        await Expect(Page.GetByTestId("product-reviews-average")).ToHaveTextAsync("3.0");
    }

    [Fact]
    public async Task Shopper_cannot_open_the_management_area()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);

        await Page.GotoAsync("/manage/products");

        await Expect(Page.GetByTestId("catalog-page")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("manage-products-page")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Shopper_cannot_see_another_customers_order()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);

        await Page.GotoAsync("/orders/ORD-SEED0001");

        await Expect(Page.GetByTestId("order-not-found")).ToContainTextAsync("not found");
        await Page.GotoAsync("/orders");
        await Expect(Page.GetByTestId("order-card-ORD-SEED0003")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("order-card-ORD-SEED0001")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Account_profile_and_password_can_be_changed()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GetByTestId("nav-account-desktop").ClickAsync();

        await Page.GetByTestId("account-name").FillAsync("Renamed Shopper");
        await Page.GetByTestId("account-city").FillAsync("Salzburg");
        await Page.GetByTestId("account-save-profile").ClickAsync();
        await Expect(Page.GetByTestId("account-profile-saved")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("nav-account-desktop")).ToHaveTextAsync("Renamed Shopper");

        await Page.GetByTestId("account-current-password").FillAsync("1234");
        await Page.GetByTestId("account-new-password").FillAsync("new-secret-1");
        await Page.GetByTestId("account-confirm-password").FillAsync("new-secret-1");
        await Page.GetByTestId("account-change-password").ClickAsync();
        await Expect(Page.GetByTestId("account-password-changed")).ToBeVisibleAsync();

        await Page.GetByTestId("nav-logout-desktop").ClickAsync();
        await LoginAsync(E2ETestSettings.Shopper.Email, "new-secret-1");
        await Expect(Page.GetByTestId("nav-account-desktop")).ToHaveTextAsync("Renamed Shopper");
    }

    [Fact]
    public async Task Manager_progresses_a_shoppers_order_and_the_shopper_sees_the_new_status()
    {
        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GotoAsync("/");
        await Page.GetByTestId("product-add-field-notebook").ClickAsync();
        await Page.GetByTestId("nav-cart-desktop").ClickAsync();
        await Page.GetByTestId("cart-checkout").ClickAsync();
        await Page.GetByTestId("checkout-confirm").ClickAsync();
        var orderNumber = await Page.GetByTestId("checkout-order-number").InnerTextAsync();
        await Page.GetByTestId("nav-logout-desktop").ClickAsync();

        await LoginAsync(E2ETestSettings.StoreManager.Email, E2ETestSettings.StoreManager.Password);
        await Page.GetByTestId("nav-orders-desktop").ClickAsync();
        await Page.GetByTestId($"order-card-{orderNumber}").ClickAsync();
        await Page.GetByTestId($"order-details-link-{orderNumber}").ClickAsync();
        await Page.GetByTestId("order-status-note").FillAsync("Packed by hand");
        await Page.GetByTestId("order-set-status-Processing").ClickAsync();
        await Expect(Page.GetByTestId("order-details-status")).ToHaveTextAsync("Processing");
        await Page.GetByTestId("order-set-status-Shipped").ClickAsync();
        await Expect(Page.GetByTestId("order-details-status")).ToHaveTextAsync("Shipped");
        await Page.GetByTestId("nav-logout-desktop").ClickAsync();

        await LoginAsync(E2ETestSettings.Shopper.Email, E2ETestSettings.Shopper.Password);
        await Page.GetByTestId("nav-orders-desktop").ClickAsync();
        await Expect(Page.GetByTestId($"order-status-{orderNumber}")).ToHaveTextAsync("Shipped");
        await Page.GotoAsync($"/orders/{orderNumber}");
        await Expect(Page.GetByTestId("order-timeline-Processing")).ToContainTextAsync("Packed by hand");
        await Expect(Page.GetByTestId("order-cancel")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Manager_can_create_a_product_that_shoppers_can_buy_and_edit_its_price()
    {
        await LoginAsync(E2ETestSettings.StoreManager.Email, E2ETestSettings.StoreManager.Password);
        await Page.GetByTestId("nav-products-desktop").ClickAsync();
        await Page.GetByTestId("product-create").ClickAsync();

        await Page.GetByTestId("product-form-id").FillAsync("travel-adapter");
        await Page.GetByTestId("product-form-name").FillAsync("Travel Adapter");
        await Page.GetByTestId("product-form-description").FillAsync("A universal plug adapter.");
        await Page.GetByTestId("product-form-new-category").FillAsync("Travel");
        await Page.GetByTestId("product-form-price").FillAsync("14.5");
        await Page.GetByTestId("product-form-stock").FillAsync("7");
        await Page.GetByTestId("product-form-save").ClickAsync();
        await Expect(Page.GetByTestId("product-row-travel-adapter")).ToBeVisibleAsync();

        await Page.GetByTestId("product-edit-travel-adapter").ClickAsync();
        await Page.GetByTestId("product-form-price").FillAsync("12");
        await Page.GetByTestId("product-form-save").ClickAsync();
        await Expect(Page.GetByTestId("product-row-travel-adapter")).ToContainTextAsync("€12.00");

        await Page.GetByTestId("nav-logout-desktop").ClickAsync();
        await Page.GotoAsync("/products/travel-adapter");
        await Expect(Page.GetByTestId("product-overview-price")).ToHaveTextAsync("€12.00");
    }

    [Fact]
    public async Task Manager_dashboard_inventory_and_coupons_are_available()
    {
        await LoginAsync(E2ETestSettings.StoreManager.Email, E2ETestSettings.StoreManager.Password);

        await Page.GetByTestId("nav-dashboard-desktop").ClickAsync();
        await Expect(Page.GetByTestId("dashboard-total-orders")).ToHaveTextAsync("3");
        await Expect(Page.GetByTestId("dashboard-out-of-stock")).ToHaveTextAsync("2");
        await Expect(Page.GetByTestId("dashboard-low-stock-canvas-tote")).ToBeVisibleAsync();

        await Page.GetByTestId("nav-coupons-desktop").ClickAsync();
        await Page.GetByTestId("coupon-form-code").FillAsync("SUMMER15");
        await Page.GetByTestId("coupon-form-value").FillAsync("15");
        await Page.GetByTestId("coupon-form-save").ClickAsync();
        await Expect(Page.GetByTestId("coupon-row-SUMMER15")).ToBeVisibleAsync();
        await Page.GetByTestId("coupon-delete-SUMMER15").ClickAsync();
        await Expect(Page.GetByTestId("coupon-row-SUMMER15")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Store_manager_sees_no_add_to_cart_buttons()
    {
        await LoginAsync(E2ETestSettings.StoreManager.Email, E2ETestSettings.StoreManager.Password);

        await Page.GotoAsync("/");

        await Expect(Page.GetByTestId("product-card-ceramic-mug")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("product-add-ceramic-mug")).ToHaveCountAsync(0);
    }

    private async Task LoginAsync(string email, string password)
    {
        await Page.GotoAsync("/login");
        await Page.GetByTestId("login-email").FillAsync(email);
        await Page.GetByTestId("login-password").FillAsync(password);
        await Page.GetByTestId("login-submit").ClickAsync();
        await Page.Locator("[data-testid='nav-logout-desktop'], [data-testid='login-error']").First.WaitForAsync();
    }

    private async Task SelectOptionAsync(string testId, string optionText)
    {
        var control = Page.GetByTestId(testId).Locator("xpath=ancestor::div[contains(concat(' ', normalize-space(@class), ' '), ' mud-input-control ')][1]");
        await control.Locator(".mud-input").ClickAsync();
        await Page.Locator(".mud-popover-open").GetByText(optionText, new() { Exact = true }).ClickAsync();
        await Expect(Page.Locator(".mud-popover-open")).ToHaveCountAsync(0);
    }
}
