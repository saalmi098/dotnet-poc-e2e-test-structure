using ShopDemo.Tests.E2E.Screenplay.Abilities;

namespace ShopDemo.Tests.E2E.Screenplay.Questions;

public sealed record ConfirmedOrderNumber : IQuestion<string>
{
    public Task<string> AnsweredByAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("checkout-order-number").InnerTextAsync();
}
