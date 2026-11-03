using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

public sealed class OrderService(ShopDbContext db, CartService carts, TimeProvider clock)
{
    public const string DeclinedCardNumber = "4000000000000002";
    public const int MaxPageSize = 50;

    public async Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        if (user.Role != UserRole.Shopper)
        {
            throw ShopException.Forbidden("Store managers cannot place customer orders.");
        }

        var customerName = Require(request.CustomerName, "Enter your name.");
        var email = AccountService.RequireValidEmail(request.Email);
        var address = Require(request.AddressLine, "Enter a street address.");
        var postalCode = Require(request.PostalCode, "Enter a postal code.");
        var city = Require(request.City, "Enter a city.");
        var country = Require(request.Country, "Enter a country.");
        if (!Enum.IsDefined(request.ShippingMethod) || !Enum.IsDefined(request.PaymentMethod))
        {
            throw new ShopException("Choose a shipping and payment method.");
        }

        var paymentReference = ValidatePayment(request);

        var cart = await carts.FindForCheckoutAsync(userId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new ShopException("Your cart is empty.");
        }

        var now = clock.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var lines = new List<OrderLine>();
        foreach (var item in cart.Items.OrderBy(item => item.Product.Name))
        {
            if (!item.Product.IsActive)
            {
                throw new ShopException($"{item.Product.Name} is no longer available. Remove it from your cart.");
            }

            var updated = await db.Products
                .Where(product => product.Id == item.ProductId && product.Stock >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, product => product.Stock - item.Quantity), cancellationToken);
            if (updated == 0)
            {
                var stock = await db.Products.AsNoTracking().Where(product => product.Id == item.ProductId)
                    .Select(product => product.Stock).FirstAsync(cancellationToken);
                throw new ShopException(stock == 0
                    ? $"{item.Product.Name} is out of stock."
                    : $"Only {stock} unit(s) of {item.Product.Name} are available.");
            }

            lines.Add(new OrderLine
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity
            });
        }

        var subtotal = Pricing.Round(lines.Sum(line => line.LineTotal));
        Coupon? coupon = null;
        if (cart.CouponCode is { } code)
        {
            coupon = await db.Coupons.FirstOrDefaultAsync(candidate => candidate.Code == code, cancellationToken);
            if (coupon is not null)
            {
                Pricing.EnsureCouponUsable(coupon, subtotal, now);
                coupon.Redemptions++;
            }
        }

        var breakdown = Pricing.Calculate(subtotal, coupon, request.ShippingMethod);
        var order = new Order
        {
            Number = OrderRules.NewOrderNumber(),
            UserId = userId,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            CustomerName = customerName,
            CustomerEmail = email,
            AddressLine = address,
            PostalCode = postalCode,
            City = city,
            Country = country,
            ShippingMethod = request.ShippingMethod,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = paymentReference,
            CouponCode = coupon?.Code,
            Subtotal = breakdown.Subtotal,
            Discount = breakdown.Discount,
            ShippingCost = breakdown.ShippingCost,
            Total = breakdown.Total,
            Lines = lines,
            History = [new OrderStatusEntry { Status = OrderStatus.Pending, At = now, Note = "Order placed" }]
        };
        db.Orders.Add(order);

        if (request.SaveAddress)
        {
            user.AddressLine = address;
            user.PostalCode = postalCode;
            user.City = city;
            user.Country = country;
        }

        db.Carts.Remove(cart);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        order.User = user;
        return order.ToDto(user);
    }

    public async Task<PagedResult<OrderDto>> ListAsync(
        int userId,
        OrderStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetUserAsync(userId, cancellationToken);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(1, page);

        var orders = db.Orders.AsNoTracking().AsQueryable();
        if (viewer.Role == UserRole.Shopper)
        {
            orders = orders.Where(order => order.UserId == userId);
        }

        if (status is { } filter)
        {
            orders = orders.Where(order => order.Status == filter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            orders = orders.Where(order =>
                order.Number.ToLower().Contains(term) ||
                order.CustomerEmail.ToLower().Contains(term) ||
                order.CustomerName.ToLower().Contains(term));
        }

        var total = await orders.CountAsync(cancellationToken);
        var items = await orders
            .Include(order => order.User)
            .Include(order => order.Lines)
            .Include(order => order.History)
            .OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>([.. items.Select(order => order.ToDto(viewer))], total, page, pageSize);
    }

    public async Task<OrderDto> GetAsync(int userId, string number, CancellationToken cancellationToken = default)
    {
        var viewer = await GetUserAsync(userId, cancellationToken);
        var order = await LoadOrderAsync(number, track: false, cancellationToken);
        EnsureVisible(order, viewer);
        return order.ToDto(viewer);
    }

    public async Task<OrderDto> CancelAsync(int userId, string number, CancellationToken cancellationToken = default)
    {
        var viewer = await GetUserAsync(userId, cancellationToken);
        var order = await LoadOrderAsync(number, track: true, cancellationToken);
        EnsureVisible(order, viewer);

        if (viewer.Role == UserRole.Shopper && !OrderRules.CanShopperCancel(order.Status))
        {
            throw new ShopException(order.Status == OrderStatus.Cancelled
                ? "This order has already been cancelled."
                : "This order has already shipped and can no longer be cancelled.");
        }

        await ChangeStatusAsync(order, OrderStatus.Cancelled, "Cancelled by customer", cancellationToken);
        return order.ToDto(viewer);
    }

    public async Task<OrderDto> UpdateStatusAsync(
        int userId,
        string number,
        OrderStatus newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetUserAsync(userId, cancellationToken);
        if (viewer.Role != UserRole.StoreManager)
        {
            throw ShopException.Forbidden("Only a store manager can change the order status.");
        }

        var order = await LoadOrderAsync(number, track: true, cancellationToken);
        await ChangeStatusAsync(order, newStatus, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), cancellationToken);
        return order.ToDto(viewer);
    }

    private async Task ChangeStatusAsync(Order order, OrderStatus newStatus, string? note, CancellationToken cancellationToken)
    {
        if (!OrderRules.CanTransition(order.Status, newStatus))
        {
            throw new ShopException($"An order that is {order.Status.ToString().ToLowerInvariant()} cannot become {newStatus.ToString().ToLowerInvariant()}.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (newStatus == OrderStatus.Cancelled)
        {
            foreach (var line in order.Lines)
            {
                await db.Products.Where(product => product.Id == line.ProductId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, product => product.Stock + line.Quantity), cancellationToken);
            }

            if (order.CouponCode is { } code)
            {
                await db.Coupons.Where(coupon => coupon.Code == code && coupon.Redemptions > 0)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(coupon => coupon.Redemptions, coupon => coupon.Redemptions - 1), cancellationToken);
            }
        }

        order.Status = newStatus;
        order.History.Add(new OrderStatusEntry { OrderId = order.Id, Status = newStatus, At = clock.GetUtcNow(), Note = note });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Order> LoadOrderAsync(string number, bool track, CancellationToken cancellationToken)
    {
        var query = db.Orders.Include(order => order.User).Include(order => order.Lines).Include(order => order.History)
            .AsSplitQuery();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        var normalized = (number ?? string.Empty).Trim().ToUpperInvariant();
        return await query.FirstOrDefaultAsync(order => order.Number == normalized, cancellationToken)
            ?? throw ShopException.NotFound($"Order {normalized} was not found.");
    }

    private static void EnsureVisible(Order order, User viewer)
    {
        // Report "not found" instead of "forbidden" so order numbers of other customers are not revealed.
        if (viewer.Role == UserRole.Shopper && order.UserId != viewer.Id)
        {
            throw ShopException.NotFound($"Order {order.Number} was not found.");
        }
    }

    private async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken) =>
        await db.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
        ?? throw ShopException.Unauthorized("Log in to continue.");

    private static string Require(string? value, string message)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? throw new ShopException(message) : trimmed;
    }

    /// <summary>Validates the (simulated) payment and returns a reference that is safe to store.</summary>
    private static string ValidatePayment(PlaceOrderRequest request)
    {
        if (request.PaymentMethod == PaymentMethod.Invoice)
        {
            return "Invoice";
        }

        Require(request.CardHolder, "Enter the name on the card.");
        var digits = (request.CardNumber ?? string.Empty).Replace(" ", "").Replace("-", "");
        if (!OrderRules.IsValidCardNumber(digits))
        {
            throw new ShopException("Enter a valid card number.");
        }

        if (!TryParseExpiry(request.CardExpiry, out var expiry))
        {
            throw new ShopException("Enter the card expiry as MM/YY.");
        }

        if (expiry < DateTime.UtcNow)
        {
            throw new ShopException("The card has expired.");
        }

        var cvc = (request.CardCvc ?? string.Empty).Trim();
        if (cvc.Length is < 3 or > 4 || !cvc.All(char.IsAsciiDigit))
        {
            throw new ShopException("Enter the 3 or 4 digit security code.");
        }

        if (digits == DeclinedCardNumber)
        {
            throw new ShopException("Your card was declined. Please use a different payment method.", 402);
        }

        var brand = digits[0] switch { '4' => "Visa", '5' => "Mastercard", '3' => "Amex", _ => "Card" };
        return $"{brand} •••• {digits[^4..]}";
    }

    private static bool TryParseExpiry(string? text, out DateTime endOfMonth)
    {
        endOfMonth = default;
        var parts = (text ?? string.Empty).Trim().Split('/');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var month) || month is < 1 or > 12 ||
            !int.TryParse(parts[1], out var year) || parts[1].Length is not (2 or 4))
        {
            return false;
        }

        if (year < 100)
        {
            year += 2000;
        }

        endOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return true;
    }
}
