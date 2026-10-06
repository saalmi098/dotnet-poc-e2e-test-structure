using ShopDemo.E2E.Screenplay.Tests.Screenplay.Abilities;

namespace ShopDemo.E2E.Screenplay.Tests.Screenplay.Tasks;

public sealed record OpenOrderHistory : ITask
{
    public Task PerformAsAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("nav-orders-desktop").ClickAsync();
}
