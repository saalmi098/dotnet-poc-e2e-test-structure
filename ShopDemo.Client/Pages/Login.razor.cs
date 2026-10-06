using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class Login
{
    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private readonly LoginModel _model = new();
    private string? _error;
    private bool _submitting;

    private async Task LoginAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            if (await Auth.LoginAsync(_model.Email, _model.Password))
            {
                var destination = ReturnUrl is "/checkout" or "/orders" or "/manage/inventory" ? ReturnUrl : "/";
                Navigation.NavigateTo(destination);
            }
            else
            {
                _error = "Those demo credentials were not recognized.";
            }
        }
        catch (Exception exception)
        {
            _error = $"Login configuration could not be loaded. {exception.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class LoginModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}