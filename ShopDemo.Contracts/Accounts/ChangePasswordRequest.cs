namespace ShopDemo.Contracts;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
