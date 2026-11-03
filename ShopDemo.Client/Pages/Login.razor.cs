using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Login
{
    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private readonly LoginModel _model = new();
    private string? _error;
    private bool _submitting;

    private string RegisterHref => string.IsNullOrEmpty(ReturnUrl)
        ? "/register"
        : $"/register?returnUrl={Uri.EscapeDataString(ReturnUrl)}";

    private async Task LoginAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            await Auth.LoginAsync(_model.Email, _model.Password);
            Navigation.NavigateTo(SafeReturnUrl(ReturnUrl));
        }
        catch (ApiException exception)
        {
            _error = exception.Message;
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class LoginModel
    {
        [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = string.Empty;
    }
}
