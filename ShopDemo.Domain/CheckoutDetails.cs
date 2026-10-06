using System.Net.Mail;

namespace ShopDemo.Domain;

public sealed record CheckoutDetails(string CustomerName, string Email)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(CustomerName);

        if (!MailAddress.TryCreate(Email, out var address) ||
            !string.Equals(address.Address, Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Enter a valid email address.", nameof(Email));
        }
    }
}
