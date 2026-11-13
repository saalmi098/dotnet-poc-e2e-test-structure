using System.Globalization;

namespace ShopDemo.Client.Services;

public static class Money
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>Formats an amount like "€12.50" independent of the browser culture.</summary>
    public static string ToMoney(this decimal amount) => "€" + amount.ToString("0.00", Culture);

    public static string ToMoney(this decimal? amount) => amount is { } value ? value.ToMoney() : string.Empty;
}
