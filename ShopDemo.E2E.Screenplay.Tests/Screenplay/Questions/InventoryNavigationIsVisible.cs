using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Questions;

public sealed record InventoryNavigationIsVisible : IQuestion<bool>
{
    public Task<bool> AnsweredByAsync(Actor actor)
        => actor.Ability<BrowseTheWeb>().Page.GetByTestId("nav-inventory-desktop").IsVisibleAsync();
}