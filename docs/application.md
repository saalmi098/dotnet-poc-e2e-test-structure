# ShopDemo application reference

This document describes the system under test: what the webshop can do, which `data-testid` values the UI exposes, and the REST API behind it. Behaviour listed here is covered by `ShopDemo.Tests` (rules and services) and can be exercised by browser tests.

## Features

| Area | What a user can do |
| --- | --- |
| Accounts | Register, log in/out (cookie session survives a reload), edit profile and default address, change password |
| Catalog | Search (name + description), filter by category / price range / in stock / on sale, sort (featured, price, name, rating, newest), paged (12 per page), sale badges, average rating |
| Product page | Overview (quantity picker, add to cart, wishlist), **Inventory** tab (deliberately slow 5 s paged/filterable grid kept as a test fixture), Reviews tab (rating histogram, write/delete review) |
| Wishlist | Heart button on cards and product page, wishlist page with "add to cart" |
| Cart | Server-side cart for guests (cookie) and accounts; guest cart is merged on login; quantity limits (stock, max 10 per line); coupon codes; free-shipping hint; totals incl. VAT |
| Checkout | Contact + shipping address (pre-filled from the profile), standard/express shipping, invoice or simulated credit card, optional "save address", stock is decremented atomically |
| Orders | History with status filter and paging, detail page with status timeline, shopper cancel (Pending/Processing; stock and coupon are returned) |
| Store manager | Dashboard, inventory (stock) editor, product CRUD (delete archives products that were ordered), coupon management, all orders with search/status filter, status transitions Pending → Processing → Shipped → Delivered (or Cancelled) with notes |

Business rules worth testing:

- Standard shipping 4.90, free from 50.00 after discounts; express 12.90; `FREESHIP` removes shipping. Prices include 20 % VAT (shown as "includes").
- Coupons are validated on apply *and* on order placement (active, not expired, redemption limit, minimum subtotal). A coupon that stops qualifying is dropped from the cart.
- Managers cannot use the cart/checkout/wishlist/reviews; shoppers cannot open `/manage/*` (redirected to the catalog), guests are redirected to `/login?returnUrl=...`.
- A shopper sees only their own orders; another shopper's order number answers "not found".
- Card `4000 0000 0000 0002` is declined and leaves cart and stock untouched; invalid Luhn numbers, past expiry, and bad CVC are rejected.
- The canvas tote starts out of stock (restock scenario); `camp-stove` is also out of stock.

## Seed data

3 users (see the README), 6 categories, 16 products (the original six come first in "featured" order), 5 coupons, 7 reviews, 1 wishlist entry, 3 historical orders (`ORD-SEED0001` delivered and `ORD-SEED0002` shipped for Jane, `ORD-SEED0003` delivered for the demo shopper). `POST /api/test/reset` restores exactly this state.

## Test ids

Dynamic ids use the product id (`{id}`, e.g. `canvas-tote`) or order number (`{number}`).

**Navigation** (inside `.mud-appbar`; desktop width): `nav-brand`, `nav-catalog-desktop`, `nav-cart-desktop` ("Cart (n)"), `nav-wishlist-desktop` (not for managers), `nav-login-desktop`, `nav-register-desktop`, `nav-orders-desktop`, `nav-account-desktop`, `nav-logout-desktop`; managers additionally `nav-dashboard-desktop`, `nav-inventory-desktop`, `nav-products-desktop`, `nav-coupons-desktop`. Mobile drawer: `nav-menu-toggle`, `nav-logout-drawer`. Footer: `site-footer`.

**Login / register**: `login-form`, `login-email`, `login-password`, `login-submit`, `login-error`, `login-register-link`; `register-form`, `register-name`, `register-email`, `register-password`, `register-confirm-password`, `register-submit`, `register-error`.

**Catalog** (`catalog-page`): `catalog-search`, `catalog-category`, `catalog-sort`, `catalog-min-price`, `catalog-max-price`, `catalog-in-stock`, `catalog-on-sale`, `catalog-clear-filters`, `catalog-result-count`, `catalog-empty`, `catalog-error`, `catalog-pagination`; per card `product-card-{id}`, `product-details-{id}`, `product-add-{id}`, `product-stock-{id}`, `product-price-{id}`, `product-original-price-{id}`, `product-sale-badge-{id}`, `product-rating-{id}`, `product-wishlist-{id}`.

**Product details** (`product-details`): tabs `product-overview-tab` / `product-inventory-tab` / `product-reviews-tab`, panels `product-overview-panel` / `product-inventory-panel` / `product-reviews-panel`; overview `product-overview-name|description|category|price|original-price|stock|rating|sale-badge|out-of-stock`, `product-details-quantity`, `product-details-add`, `product-details-wishlist`, `product-not-found`; inventory `product-inventory-loading|grid|pager|row-{n}|id|stock|availability` (+`-{n}` suffix from row 2); reviews `product-reviews-average|rating|count-{1..5}`, `review-form`, `review-rating`, `review-title`, `review-comment`, `review-submit`, `review-error`, `review-login-hint`, `review-already-reviewed`, `reviews-empty`, `review-list`, `review-{reviewId}`, `review-title-{reviewId}`, `review-text-{reviewId}`, `review-delete-{reviewId}`.

**Cart** (`cart-page`): `cart-empty`, `cart-item-{id}`, `cart-item-name-{id}`, `cart-quantity-{id}` (wrapper around the numeric field), `cart-line-total-{id}`, `cart-item-warning-{id}`, `cart-remove-{id}`, `cart-summary`, `cart-item-count`, `cart-subtotal`, `cart-discount`, `cart-shipping`, `cart-total`, `cart-vat`, `cart-free-shipping-hint`, `cart-coupon-input`, `cart-coupon-apply`, `cart-coupon-remove`, `cart-coupon-code`, `cart-coupon-error`, `cart-checkout`, `cart-manager-hint`.

**Checkout** (`checkout-page`): `checkout-form`, `checkout-name`, `checkout-email`, `checkout-address`, `checkout-postal-code`, `checkout-city`, `checkout-country`, `checkout-save-address`, `checkout-shipping` (+`-standard`, `-express`), `checkout-payment` (+`-invoice`, `-card`), `checkout-card-fields`, `checkout-card-holder|number|expiry|cvc`, `checkout-confirm`, `checkout-error`, `checkout-empty`, summary `checkout-summary`, `checkout-line-{id}`, `checkout-subtotal|discount|shipping-cost|total`; confirmation `checkout-success`, `checkout-order-number`, `checkout-order-total`, `checkout-view-order`, `checkout-view-orders`.

**Orders**: list `orders-page`, `orders-scope-hint`, `orders-status-filter`, `orders-search` (manager), `orders-empty`, `orders-error`, `orders-pagination`, `order-card-{number}`, `order-number-{number}`, `order-status-{number}`, `order-total-{number}`, `order-details-link-{number}`; detail `order-details-page`, `order-details-number|status|subtotal|discount|shipping|total|payment`, `order-lines`, `order-line-{id}`, `order-address`, `order-timeline`, `order-timeline-{Status}`, `order-cancel`, `order-cancel-confirm`, `order-cancel-abort`, `order-manage-panel`, `order-status-note`, `order-set-status-{Processing|Shipped|Delivered|Cancelled}`, `order-not-found`.

**Account / wishlist**: `account-page`, `account-email`, `account-role`, `account-name`, `account-phone`, `account-address`, `account-postal-code`, `account-city`, `account-country`, `account-save-profile`, `account-profile-saved`, `account-profile-error`, `account-current-password`, `account-new-password`, `account-confirm-password`, `account-change-password`, `account-password-changed`, `account-password-error`; `wishlist-page`, `wishlist-empty`, `wishlist-item-{id}`, `wishlist-name-{id}`, `wishlist-price-{id}`, `wishlist-stock-{id}`, `wishlist-remove-{id}`, `wishlist-add-to-cart-{id}`.

**Manager**: `dashboard-page`, `dashboard-total-orders|open-orders|revenue|customers|products|low-stock|out-of-stock`, `dashboard-low-stock-list`, `dashboard-low-stock-{id}`; `inventory-page`, `inventory-item-{id}`, `inventory-stock-{id}`, `inventory-save-{id}`, `inventory-current-stock-{id}`; `manage-products-page`, `manage-products-filter`, `manage-products-table`, `product-create`, `product-row-{id}`, `product-row-stock-{id}`, `product-row-status-{id}`, `product-edit-{id}`, `product-delete-{id}`, `product-delete-confirm`, `product-delete-abort`; `product-form-page`, `product-form`, `product-form-id|name|description|category|price|original-price|stock|image|active|save|error`; `coupons-page`, `coupons-table`, `coupon-row-{code}`, `coupon-used-{code}`, `coupon-toggle-{code}`, `coupon-delete-{code}`, `coupon-form`, `coupon-form-code|description|type|value|min-subtotal|max-redemptions|expires|save|error`.

## REST API

All endpoints are under `/api`. Errors are RFC 7807 problem documents whose `detail` is user-presentable. Authentication is the `shopdemo.auth` cookie; guests are tracked by the `shopdemo.guest` cookie for carts.

| Method and path | Auth | Purpose |
| --- | --- | --- |
| `GET /health` | - | Liveness |
| `POST /auth/register`, `POST /auth/login`, `POST /auth/logout`, `GET /auth/me` | - | Session (`/me` answers 204 for guests) |
| `PUT /auth/profile`, `POST /auth/change-password` | user | Account |
| `GET /products?q&category&minPrice&maxPrice&inStock&onSale&sort&page&pageSize` | - | Catalog (`sort`: Featured, PriceAscending, PriceDescending, NameAscending, TopRated, Newest) |
| `GET /categories`, `GET /products/{id}` | - | Catalog |
| `GET/POST /products/{id}/reviews`, `DELETE /reviews/{id}` | read: -, write: user | Reviews |
| `GET /wishlist`, `PUT/DELETE /wishlist/{productId}` | user | Wishlist |
| `GET /cart?shipping=`, `POST /cart/items`, `PUT/DELETE /cart/items/{productId}`, `DELETE /cart`, `POST/DELETE /cart/coupon` | - | Cart (guest cookie or account) |
| `POST /orders`, `GET /orders?status&search&page&pageSize`, `GET /orders/{number}`, `POST /orders/{number}/cancel` | user | Orders |
| `PUT /orders/{number}/status` | manager | Fulfilment |
| `GET /admin/dashboard`, `GET/POST /admin/products`, `GET/PUT/DELETE /admin/products/{id}`, `PUT /admin/products/{id}/stock`, `GET/POST /admin/coupons`, `PUT/DELETE /admin/coupons/{code}` | manager | Administration |
| `POST /test/reset` | - (if enabled) | Restore seed data |
