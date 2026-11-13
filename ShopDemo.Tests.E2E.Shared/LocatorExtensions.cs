using Microsoft.Playwright;

namespace ShopDemo.Tests.E2E.Shared;

public static class LocatorExtensions
{
    /// <summary>
    /// Waits up to <paramref name="timeoutMs"/> for the locator to become visible and reports the outcome.
    /// Lists in the app are loaded from the API after navigation, so a bare count right after a click can race.
    /// </summary>
    public static async Task<bool> IsEventuallyVisibleAsync(this ILocator locator, float timeoutMs = 10_000)
    {
        try
        {
            await locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
