using ShopDemo.Domain;

namespace ShopDemo.Client.Pages;

public partial class OrderHistory
{
    private IReadOnlyList<Order> _orders = [];

    protected override void OnInitialized()
    {
        if (Auth.CurrentUser is null)
        {
            Navigation.NavigateTo("/login?returnUrl=%2Forders");
            return;
        }

        _orders = Checkout.GetOrdersForCurrentUser();
    }
}