namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

public sealed class PageObjectBindingException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
