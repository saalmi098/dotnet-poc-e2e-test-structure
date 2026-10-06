namespace ShopDemo.Domain;

public enum DemoRole
{
    Shopper,
    StoreManager
}

public sealed record DemoUser(string Email, string DisplayName, DemoRole Role = DemoRole.Shopper);
