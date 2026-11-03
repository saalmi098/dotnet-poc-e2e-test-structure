using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopDemo.Contracts;
using ShopDemo.Domain;
using ShopDemo.Infrastructure.Data;

namespace ShopDemo.Infrastructure.Services;

public sealed class AccountService(ShopDbContext db, TimeProvider clock)
{
    public const int MinPasswordLength = 8;
    private static readonly PasswordHasher<User> Hasher = new();

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.DisplayName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ShopException("Enter your name.");
        }

        var email = RequireValidEmail(request.Email);
        if ((request.Password ?? string.Empty).Length < MinPasswordLength)
        {
            throw new ShopException($"The password must be at least {MinPasswordLength} characters long.");
        }

        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw ShopException.Conflict("An account with this email address already exists.");
        }

        var user = new User
        {
            Email = email,
            DisplayName = name,
            Role = UserRole.Shopper,
            CreatedAt = clock.GetUtcNow()
        };
        user.PasswordHash = Hasher.HashPassword(user, request.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }

    /// <summary>Returns the account when the credentials match; otherwise null.</summary>
    public async Task<UserDto?> AuthenticateAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Email == normalized, cancellationToken);
        if (user is null || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user.ToDto();
    }

    public async Task<UserDto?> FindAsync(int userId, CancellationToken cancellationToken = default) =>
        (await db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken))?.ToDto();

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ShopException.NotFound("Account not found.");

        var name = (request.DisplayName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ShopException("Enter your name.");
        }

        user.DisplayName = name;
        user.Phone = Clean(request.Phone);
        user.AddressLine = Clean(request.AddressLine);
        user.PostalCode = Clean(request.PostalCode);
        user.City = Clean(request.City);
        user.Country = Clean(request.Country);
        await db.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw ShopException.NotFound("Account not found.");

        if (Hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword ?? string.Empty)
            == PasswordVerificationResult.Failed)
        {
            throw new ShopException("The current password is not correct.");
        }

        if ((request.NewPassword ?? string.Empty).Length < MinPasswordLength)
        {
            throw new ShopException($"The new password must be at least {MinPasswordLength} characters long.");
        }

        user.PasswordHash = Hasher.HashPassword(user, request.NewPassword!);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static string RequireValidEmail(string? email)
    {
        var trimmed = (email ?? string.Empty).Trim();
        if (!MailAddress.TryCreate(trimmed, out var address) ||
            !string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ShopException("Enter a valid email address.");
        }

        return trimmed.ToLowerInvariant();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
