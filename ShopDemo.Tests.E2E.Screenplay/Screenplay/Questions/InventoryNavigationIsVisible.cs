using ShopDemo.Tests.E2E.Screenplay.Abilities;

namespace ShopDemo.Tests.E2E.Screenplay.Questions;

public sealed record InventoryNavigationIsVisible : IQuestion<bool>
{
    public Task<bool> AnsweredByAsync(Actor actor)
        => actor.Ability<BrowseTheWeb>().Page.GetByTestId("nav-inventory-desktop").IsVisibleAsync();
}