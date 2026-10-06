namespace ShopDemo.E2E.Screenplay.Tests.Screenplay;

public interface IQuestion<T>
{
    Task<T> AnsweredByAsync(Actor actor);
}
