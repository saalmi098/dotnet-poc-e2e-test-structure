using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Questions;

public sealed record OrderIsVisible(string OrderNumber) : IQuestion<bool>
{
    public async Task<bool> AnsweredByAsync(Actor actor) =>
        await actor.Ability<BrowseTheWeb>()
            .Page.GetByTestId($"order-card-{OrderNumber}")
            .CountAsync() > 0;
}
