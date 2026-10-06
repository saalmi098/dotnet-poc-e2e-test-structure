using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Questions;

public sealed record ConfirmedOrderNumber : IQuestion<string>
{
    public Task<string> AnsweredByAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("checkout-order-number").InnerTextAsync();
}
