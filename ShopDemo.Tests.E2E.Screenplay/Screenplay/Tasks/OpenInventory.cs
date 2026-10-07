using ShopDemo.Tests.E2E.Screenplay.Abilities;

namespace ShopDemo.Tests.E2E.Screenplay.Tasks;

public sealed record OpenInventory : ITask
{
    public Task PerformAsAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GotoAsync("/manage/inventory");
}
