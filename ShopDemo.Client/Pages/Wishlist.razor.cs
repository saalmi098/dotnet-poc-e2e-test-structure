using ShopDemo.Contracts.Catalog;

namespace ShopDemo.Client.Pages;

public partial class Wishlist
{
    private IReadOnlyList<ProductSummaryDto> _items = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectToLoginIfAnonymous("/wishlist"))
        {
            return;
        }

        if (Auth.IsManager)
        {
            Navigation.NavigateTo("/");
            return;
        }

        await TryAsync(async () => _items = await Api.GetWishlistAsync());
        _loading = false;
    }

    private async Task RemoveAsync(ProductSummaryDto product)
    {
        if (await TryAsync(() => Api.RemoveFromWishlistAsync(product.Id), $"{product.Name} removed from your wishlist."))
        {
            _items = [.. _items.Where(item => item.Id != product.Id)];
        }
    }
}
