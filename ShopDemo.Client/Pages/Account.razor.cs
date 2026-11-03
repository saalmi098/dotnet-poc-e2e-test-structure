using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Account
{
    private readonly ProfileModel _profile = new();
    private readonly PasswordModel _password = new();
    private string? _profileError;
    private bool _profileSaved;
    private string? _passwordError;
    private bool _passwordChanged;

    protected override void OnInitialized()
    {
        if (RedirectToLoginIfAnonymous("/account"))
        {
            return;
        }

        var user = Auth.CurrentUser!;
        _profile.DisplayName = user.DisplayName;
        _profile.Phone = user.Phone;
        _profile.AddressLine = user.AddressLine;
        _profile.PostalCode = user.PostalCode;
        _profile.City = user.City;
        _profile.Country = user.Country;
    }

    private async Task SaveProfileAsync()
    {
        _profileError = null;
        _profileSaved = false;
        try
        {
            var user = await Api.UpdateProfileAsync(new UpdateProfileRequest(
                _profile.DisplayName, _profile.Phone, _profile.AddressLine, _profile.PostalCode, _profile.City, _profile.Country));
            Auth.UpdateUser(user);
            _profileSaved = true;
        }
        catch (ApiException exception)
        {
            _profileError = exception.Message;
        }
    }

    private async Task ChangePasswordAsync()
    {
        _passwordError = null;
        _passwordChanged = false;
        try
        {
            await Api.ChangePasswordAsync(new ChangePasswordRequest(_password.CurrentPassword, _password.NewPassword));
            _password.CurrentPassword = _password.NewPassword = _password.ConfirmPassword = string.Empty;
            _passwordChanged = true;
        }
        catch (ApiException exception)
        {
            _passwordError = exception.Message;
        }
    }

    private sealed class ProfileModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        public string DisplayName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AddressLine { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    private sealed class PasswordModel
    {
        [Required(ErrorMessage = "Enter your current password.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a new password."), MinLength(8, ErrorMessage = "The new password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
