namespace ShopDemo.Tests.E2E.Pom.V2.Infrastructure.POM;

public sealed class PageObjectBindingException : InvalidOperationException
{
    public PageObjectBindingException(string message)
        : base(message)
    {
    }

    public PageObjectBindingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
