namespace ShopDemo.E2E.Screenplay.Tests.Screenplay;

public sealed record ConfirmedOrderNumber : IQuestion<string>
{
    public Task<string> AnsweredByAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("checkout-order-number").InnerTextAsync();
}

public sealed record OrderIsVisible(string OrderNumber) : IQuestion<bool>
{
    public async Task<bool> AnsweredByAsync(Actor actor) =>
        await actor.Ability<BrowseTheWeb>()
            .Page.GetByTestId($"order-card-{OrderNumber}")
            .CountAsync() > 0;
}

public sealed record InventoryIsVisible : IQuestion<bool>
{
    public Task<bool> AnsweredByAsync(Actor actor) =>
        actor.Ability<BrowseTheWeb>().Page.GetByTestId("inventory-page").IsVisibleAsync();
}
