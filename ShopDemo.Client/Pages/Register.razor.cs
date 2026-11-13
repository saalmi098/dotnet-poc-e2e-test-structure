using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Register
{
    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private readonly RegisterModel _model = new();
    private string? _error;
    private bool _submitting;

    private async Task RegisterAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            await Auth.RegisterAsync(new RegisterRequest(_model.DisplayName, _model.Email, _model.Password));
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

    private sealed class RegisterModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a password."), MinLength(8, ErrorMessage = "The password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
