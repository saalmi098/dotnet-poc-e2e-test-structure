namespace ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

/// <summary>
/// Wraps a locator that must be visible before the owning component is ready.
/// The type itself is the only documentation a reader needs: Required means WaitReady()/IsReady()
/// depend on it.
/// </summary>
public readonly struct Required<T>(T value)
{
    public T Value { get; } = value;
}

/// <summary>
/// Wraps a locator that may legitimately be absent (role- or state-gated UI). Never participates
/// in WaitReady()/IsReady().
/// </summary>
public readonly struct Optional<T>(T value)
{
    public T Value { get; } = value;
}
