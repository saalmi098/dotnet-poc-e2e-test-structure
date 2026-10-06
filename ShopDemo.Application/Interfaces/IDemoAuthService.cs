using ShopDemo.Domain;

namespace ShopDemo.Application.Interfaces;

public interface IDemoAuthService
{
    DemoUser? CurrentUser { get; }
    event Action? AuthenticationStateChanged;
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    void Logout();
}
