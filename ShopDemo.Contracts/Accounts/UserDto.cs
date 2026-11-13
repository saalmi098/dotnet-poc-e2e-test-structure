using ShopDemo.Domain;

namespace ShopDemo.Contracts;

public sealed record UserDto(
    int Id,
    string Email,
    string DisplayName,
    UserRole Role,
    string? Phone,
    string? AddressLine,
    string? PostalCode,
    string? City,
    string? Country);
