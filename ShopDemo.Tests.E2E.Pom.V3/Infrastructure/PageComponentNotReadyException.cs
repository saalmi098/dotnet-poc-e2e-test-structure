namespace ShopDemo.Tests.E2E.Pom.V3.Infrastructure;

public sealed class PageComponentNotReadyException(string message, Exception innerException)
    : InvalidOperationException(message, innerException);
