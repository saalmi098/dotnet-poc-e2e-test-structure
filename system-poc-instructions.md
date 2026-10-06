Act as a senior .NET developer. Build a small, runnable proof-of-concept webshop that demonstrates clean architecture and a few useful user flows.

## Technology
- C#
- Blazor WebAssembly
- MudBlazor
- Use a supported .NET SDK available in the environment. If starting from scratch, state which SDK and MudBlazor versions you chose.
- Do not add a database, backend API, ORM, or external payment provider.

## Product goal
Create a simple, responsive webshop where a user can browse products, add them to a cart, and complete a simulated checkout. The purpose is to experiment with the technology and architecture, not to build a production commerce system.

## Architecture
Keep the design clean, understandable, and appropriately lightweight. Separate UI, application logic, domain concepts, and infrastructure/configuration concerns. A reasonable starting structure is:

- `ShopDemo.Domain` — core models and domain rules
- `ShopDemo.Application` — use cases and interfaces/abstractions
- `ShopDemo.Infrastructure` — implementations that read configuration and manage demo state
- `ShopDemo.Client` — Blazor WebAssembly UI and MudBlazor components
- `ShopDemo.Tests` — unit tests for important domain/application behavior

Adjust the structure if you have a good reason, but explain the choice. Keep dependencies pointing inward: the Domain must not depend on Blazor, MudBlazor, or Infrastructure. Avoid unnecessary abstractions and boilerplate.

## Configuration and demo limitations
- Define the sample products in a JSON configuration file shipped with the client.
- Define demo login credentials in a separate configuration file shipped with the client.
- Use no database or backend persistence. Cart contents, login state, and demo order history may live in memory and reset on page refresh.
- Do not implement real payments. Checkout should create a simulated order and show a confirmation.
- Clearly document that this is a client-only PoC: configuration and hardcoded credentials are visible to users and must never be treated as secure or used for real authentication.

Include a small, realistic sample catalog with product name, description, category, price, image URL or local placeholder, and stock/availability.

## User flows and features
1. **Catalog**
   - Browse products in a responsive layout.
   - Search by name and filter by category.
   - View basic product details.
   - Show a clear unavailable/out-of-stock state if applicable.

2. **Cart**
   - Add products to the cart.
   - Change quantities and remove items.
   - Show item count and subtotal.
   - Prevent invalid quantities or quantities beyond the configured stock.

3. **Demo login**
   - Provide a simple login and logout flow using the configured demo credentials.
   - Show useful validation and error messages.
   - Protect checkout and the order-history page from unauthenticated users.
   - Keep the login implementation explicitly demo-only; do not imply it provides real security.

4. **Simulated checkout**
   - Collect a small set of details, such as customer name and email.
   - Validate inputs.
   - Show an order summary before submission.
   - On confirmation, create an in-memory order, clear the cart, and show an order confirmation with a generated order number.

5. **Order history**
   - Let the logged-in demo user see orders created during the current session.
   - Clearly indicate that this history is not persisted.

Use MudBlazor components consistently, with clear navigation, loading/error/empty states, and a usable mobile layout. Keep the UI simple rather than spending time on elaborate styling.

## Implementation expectations
- Use dependency injection and interfaces where they help keep layers independent.
- Load catalog and demo-user configuration through a small, testable infrastructure service.
- Keep business rules out of Razor components.
- Use clear naming and concise comments only where they add value.
- Include sensible error handling if configuration cannot be loaded or is invalid.
- Add unit tests for meaningful rules, such as cart quantity validation and checkout/order creation.
- Do not add features that require a backend or make the project feel production-ready.

## Deliverables
1. Create the complete solution and all required source/configuration files in the repository.
2. Include a README with setup/run instructions, architecture overview, demo credentials, and PoC/security limitations.
3. Build the solution and run the tests. Fix any errors you find.
4. At the end, summarize the project structure, key design decisions, how to run it, and any known limitations.

If you are working in an existing repository, inspect it first and preserve its conventions where practical. If you cannot execute a command or verify something, say so rather than claiming it succeeded. Do not return pseudocode or omit essential files.