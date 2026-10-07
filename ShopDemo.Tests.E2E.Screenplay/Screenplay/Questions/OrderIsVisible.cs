using ShopDemo.Tests.E2E.Screenplay.Abilities;

namespace ShopDemo.Tests.E2E.Screenplay.Questions;

public sealed record OrderIsVisible(string OrderNumber) : IQuestion<bool>
{
    public async Task<bool> AnsweredByAsync(Actor actor) =>
        await actor.Ability<BrowseTheWeb>()
            .Page.GetByTestId($"order-card-{OrderNumber}")
            .CountAsync() > 0;
}
