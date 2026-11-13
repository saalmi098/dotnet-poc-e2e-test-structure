using ShopDemo.Contracts.Orders;

namespace ShopDemo.Client.Pages;

public partial class OrderDetails
{
    [Parameter]
    public string Number { get; set; } = string.Empty;

    private OrderDto? _order;
    private bool _loading = true;
    private bool _confirmCancel;
    private string? _note;
    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        if (RedirectToLoginIfAnonymous($"/orders/{Number}"))
        {
            return;
        }

        _loading = true;
        _order = null;
        _error = null;
        _confirmCancel = false;
        try
        {
            _order = await Api.GetOrderAsync(Number);
        }
        catch (ApiException exception)
        {
            _error = exception.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CancelAsync()
    {
        if (await TryAsync(async () => _order = await Api.CancelOrderAsync(Number), "Your order was cancelled."))
        {
            _confirmCancel = false;
        }
    }

    private async Task UpdateStatusAsync(OrderStatus status)
    {
        if (await TryAsync(async () => _order = await Api.UpdateOrderStatusAsync(Number, status, _note), $"Order marked as {status}."))
        {
            _note = null;
        }
    }
}
