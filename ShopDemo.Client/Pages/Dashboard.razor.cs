namespace ShopDemo.Client.Pages;

public partial class Dashboard
{
    private const int AdminLowStockThreshold = 5;

    private DashboardDto? _dashboard;
    private string? _error;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectUnlessManager("/manage"))
        {
            return;
        }

        try
        {
            _dashboard = await Api.GetDashboardAsync();
        }
        catch (ApiException exception)
        {
            _error = $"The dashboard could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }
}
