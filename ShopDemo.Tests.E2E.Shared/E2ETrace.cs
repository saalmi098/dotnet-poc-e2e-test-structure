using System.Diagnostics;

namespace ShopDemo.Tests.E2E.Shared;

public static class E2ETrace
{
    private static bool Enabled => true;

    public static void Write(Type source, string message, Action<string>? additionalOutput = null)
    {
        if (!Enabled) return;

        ArgumentNullException.ThrowIfNull(source);

        var sourceName = source.FullName ?? source.Name;
        var output = $"[{sourceName}] {message}";
        Debug.WriteLine(output);
        additionalOutput?.Invoke(output);
    }
}
