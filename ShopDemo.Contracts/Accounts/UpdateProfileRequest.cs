namespace ShopDemo.Contracts;

public sealed record UpdateProfileRequest(
    string DisplayName,
    string? Phone,
    string? AddressLine,
    string? PostalCode,
    string? City,
    string? Country);
