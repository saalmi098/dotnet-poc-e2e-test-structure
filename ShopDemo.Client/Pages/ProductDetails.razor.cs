using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShopDemo.Domain;

namespace ShopDemo.Client.Pages;

public partial class ProductDetails : IDisposable
{
    private static readonly TimeSpan InventoryLoadDelay = TimeSpan.FromSeconds(5);
    private const int InventoryTabIndex = 1;
    private const int InventoryRowCount = 25;

    [Parameter]
    public string ProductId { get; set; } = string.Empty;

    private Product? _product;
    private bool _loading = true;
    private int _activePanelIndex;
    private bool _inventoryLoadStarted;
    private bool _inventoryLoading;
    private IReadOnlyList<InventoryGridRow> _inventoryRows = [];
    private string? _inventoryLoadError;
    private CancellationTokenSource? _inventoryLoadCancellation;

    protected override async Task OnParametersSetAsync()
    {
        if (_inventoryLoadCancellation is not null)
        {
            await _inventoryLoadCancellation.CancelAsync();
        }

        _inventoryLoadCancellation?.Dispose();
        _inventoryLoadCancellation = new CancellationTokenSource();

        _loading = true;
        _product = null;
        _activePanelIndex = 0;
        _inventoryLoadStarted = false;
        _inventoryLoading = false;
        _inventoryRows = [];
        _inventoryLoadError = null;

        try
        {
            _product = await Catalog.GetProductAsync(ProductId);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Product details could not be loaded. {exception.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private Task OnActivePanelIndexChanged(int panelIndex)
    {
        _activePanelIndex = panelIndex;
        if (panelIndex == InventoryTabIndex && !_inventoryLoadStarted && _product is not null)
        {
            _inventoryLoadStarted = true;
            _inventoryLoading = true;
            // Let tab selection return while the required grid locators wait for this data.
            _ = LoadInventoryRows(_product, _inventoryLoadCancellation?.Token ?? CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    private async Task LoadInventoryRows(Product product, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(InventoryLoadDelay, cancellationToken);

            var availability = product.IsAvailable ? "Available" : "Out of stock";
            _inventoryRows = Enumerable.Range(1, InventoryRowCount)
                .Select(sequence => new InventoryGridRow(sequence, product.Id, product.Stock, availability))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The load was canceled, so we don't need to do anything.
        }
        catch (Exception exception)
        {
            _inventoryLoadError = $"Inventory details could not be loaded. {exception.Message}";
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _inventoryLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private static string GetInventoryCellTestId(string prefix, int sequence)
        => sequence == 1 ? prefix : $"{prefix}-{sequence}";

    private async Task AddToCartAsync()
    {
        try
        {
            await Cart.AddProductAsync(ProductId);
            Snackbar.Add("Added to your cart.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    public void Dispose()
    {
        _inventoryLoadCancellation?.Cancel();
        _inventoryLoadCancellation?.Dispose();
    }

    private sealed record InventoryGridRow(int Sequence, string ProductId, int Stock, string Availability);
}