namespace ShopDemo.Contracts;

public sealed record RegisterRequest(string DisplayName, string Email, string Password);
