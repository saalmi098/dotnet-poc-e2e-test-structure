using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Questions;

public sealed record InventoryIsVisible : IQuestion<bool>
{
    public Task<bool> AnsweredByAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("inventory-page").IsVisibleAsync();
}
