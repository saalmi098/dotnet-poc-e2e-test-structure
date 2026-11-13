using ShopDemo.Contracts.Catalog;
using ShopDemo.Contracts.Orders;

namespace ShopDemo.Client.Pages;

public partial class OrderHistory
{
    private const int PageSize = 20;

    private PagedResult<OrderDto>? _result;
    private OrderStatus? _status;
    private string? _search;
    private int _page = 1;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectToLoginIfAnonymous("/orders"))
        {
            return;
        }

        await LoadAsync();
    }

    private async Task OnFilterChangedAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    private async Task OnPageChangedAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _result = await Api.GetOrdersAsync(_status, _search, _page, PageSize);
            _error = null;
        }
        catch (ApiException exception)
        {
            _error = $"Orders could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }
}
