using ShopDemo.Contracts;
using ShopDemo.Domain;

namespace ShopDemo.Client.Services;

/// <summary>The signed-in account as seen by the UI. The server owns the actual session (cookie).</summary>
public sealed class AuthState(ShopApi api)
{
    public UserDto? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsManager => CurrentUser?.Role == UserRole.StoreManager;
    public bool IsShopper => CurrentUser?.Role == UserRole.Shopper;

    public event Action? Changed;

    /// <summary>Restores the session after a page reload.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            CurrentUser = await api.GetCurrentUserAsync();
        }
        catch (ApiException)
        {
            CurrentUser = null;
        }
    }

    public async Task LoginAsync(string email, string password)
    {
        CurrentUser = await api.LoginAsync(email, password);
        Changed?.Invoke();
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        CurrentUser = await api.RegisterAsync(request);
        Changed?.Invoke();
    }

    public async Task LogoutAsync()
    {
        try
        {
            await api.LogoutAsync();
        }
        finally
        {
            CurrentUser = null;
            Changed?.Invoke();
        }
    }

    public void UpdateUser(UserDto user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }
}
