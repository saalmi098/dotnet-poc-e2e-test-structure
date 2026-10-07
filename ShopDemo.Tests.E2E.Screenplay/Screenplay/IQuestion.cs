namespace ShopDemo.Tests.E2E.Screenplay;

public interface IQuestion<T>
{
    Task<T> AnsweredByAsync(Actor actor);
}
