using System.Text.Json;
using System.Net.Http.Json;
using ShopDemo.Application;
using ShopDemo.Domain;

namespace ShopDemo.Infrastructure;

public sealed class DemoAuthService(HttpClient httpClient) : IDemoAuthService
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<DemoCredential>? _credentials;

    public DemoUser? CurrentUser { get; private set; }

    public event Action? AuthenticationStateChanged;

    public async Task<bool> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);
        var credentials = await GetCredentialsAsync(cancellationToken);
        var match = credentials.FirstOrDefault(credential =>
            string.Equals(credential.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(credential.Password, password, StringComparison.Ordinal));

        if (match is null)
        {
            return false;
        }

        CurrentUser = new DemoUser(match.Email, match.DisplayName, match.Role);
        AuthenticationStateChanged?.Invoke();
        return true;
    }

    public void Logout()
    {
        if (CurrentUser is null)
        {
            return;
        }

        CurrentUser = null;
        AuthenticationStateChanged?.Invoke();
    }

    private async Task<IReadOnlyList<DemoCredential>> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        if (_credentials is not null)
        {
            return _credentials;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_credentials is not null)
            {
                return _credentials;
            }

            DemoCredentialConfiguration[]? configurations;
            try
            {
                configurations = await httpClient.GetFromJsonAsync<DemoCredentialConfiguration[]>(
                    "data/demo-users.json",
                    cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The demo-user configuration JSON is invalid.", exception);
            }

            if (configurations is null || configurations.Length == 0)
            {
                throw new InvalidDataException("The demo-user configuration must contain at least one account.");
            }

            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var credentials = new List<DemoCredential>(configurations.Length);
            foreach (var configuration in configurations)
            {
                if (configuration is null ||
                    string.IsNullOrWhiteSpace(configuration.Email) ||
                    string.IsNullOrWhiteSpace(configuration.Password) ||
                    string.IsNullOrWhiteSpace(configuration.DisplayName) ||
                    !Enum.TryParse<DemoRole>(configuration.Role, ignoreCase: true, out var role) ||
                    !Enum.IsDefined(role))
                {
                    throw new InvalidDataException("The demo-user configuration contains incomplete credentials or an unsupported role.");
                }

                if (!emails.Add(configuration.Email))
                {
                    throw new InvalidDataException($"The demo-user configuration contains duplicate email '{configuration.Email}'.");
                }

                credentials.Add(new DemoCredential(
                    configuration.Email,
                    configuration.Password,
                    configuration.DisplayName,
                    role));
            }

            _credentials = credentials.AsReadOnly();
            return _credentials;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private sealed record DemoCredential(string Email, string Password, string DisplayName, DemoRole Role);

    private sealed class DemoCredentialConfiguration
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }
}
