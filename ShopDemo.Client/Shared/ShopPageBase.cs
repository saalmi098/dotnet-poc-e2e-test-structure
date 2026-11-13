using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShopDemo.Client.Services;

namespace ShopDemo.Client.Shared;

/// <summary>Common services and guard helpers for the shop pages.</summary>
public abstract class ShopPageBase : ComponentBase
{
    [Inject] protected ShopApi Api { get; set; } = default!;
    [Inject] protected AuthState Auth { get; set; } = default!;
    [Inject] protected CartState Basket { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>Sends anonymous visitors to the login page; returns true when it redirected.</summary>
    protected bool RedirectToLoginIfAnonymous(string returnUrl)
    {
        if (Auth.IsAuthenticated)
        {
            return false;
        }

        Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return true;
    }

    /// <summary>Sends anyone but a store manager away; returns true when it redirected.</summary>
    protected bool RedirectUnlessManager(string returnUrl)
    {
        if (RedirectToLoginIfAnonymous(returnUrl))
        {
            return true;
        }

        if (Auth.IsManager)
        {
            return false;
        }

        Navigation.NavigateTo("/");
        return true;
    }

    /// <summary>Runs an API action and reports failures in a snackbar. Returns true on success.</summary>
    protected async Task<bool> TryAsync(Func<Task> action, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage is not null)
            {
                Snackbar.Add(successMessage, Severity.Success);
            }

            return true;
        }
        catch (ApiException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
            return false;
        }
    }

    protected async Task AddToCartAsync(string productId, int quantity = 1) =>
        await TryAsync(() => Basket.AddAsync(productId, quantity), "Added to your cart.");

    /// <summary>Only same-site relative paths are valid redirect targets (prevents open redirects).</summary>
    public static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl[0] == '/' && !returnUrl.StartsWith("//") && !returnUrl.Contains(Convert.ToChar(92))
            ? returnUrl
            : "/";
}
