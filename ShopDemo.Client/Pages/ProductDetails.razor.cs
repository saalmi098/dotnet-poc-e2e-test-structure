using ShopDemo.Contracts.Catalog;

namespace ShopDemo.Client.Pages;

public partial class ProductDetails : IDisposable
{
    private static readonly TimeSpan InventoryLoadDelay = TimeSpan.FromSeconds(5);
    private const int InventoryTabIndex = 1;
    private const int InventoryRowCount = 25;

    [Parameter]
    public string ProductId { get; set; } = string.Empty;

    private ProductDetailDto? _product;
    private IReadOnlyList<ReviewDto> _reviews = [];
    private bool _loading = true;
    private int _activePanelIndex;
    private int _quantity = 1;
    private bool _inventoryLoadStarted;
    private bool _inventoryLoading;
    private IReadOnlyList<InventoryGridRow> _inventoryRows = [];
    private string? _inventoryLoadError;
    private CancellationTokenSource? _inventoryLoadCancellation;

    private int _reviewRating = 5;
    private string? _reviewTitle;
    private string? _reviewComment;
    private string? _reviewError;

    private int MaxQuantity => Math.Min(_product?.Stock ?? 1, Pricing.MaxQuantityPerLine);

    private bool HasReviewed => Auth.CurrentUser is { } user && _reviews.Any(review => review.UserId == user.Id);

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
        _reviews = [];
        _activePanelIndex = 0;
        _quantity = 1;
        _inventoryLoadStarted = false;
        _inventoryLoading = false;
        _inventoryRows = [];
        _inventoryLoadError = null;

        try
        {
            _product = await Api.GetProductAsync(ProductId);
            _reviews = await Api.GetReviewsAsync(ProductId);
        }
        catch (ApiException exception) when (exception.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            Snackbar.Add($"Product details could not be loaded. {exception.Message}", Severity.Error);
        }
        catch (ApiException)
        {
            // Unknown product: the page shows the "could not be found" message.
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

    // The inventory tab is a deliberate test fixture: a slow-loading, paged and filterable grid that
    // gives E2E tests something realistic to wait for. It does not read the inventory from the server.
    private async Task LoadInventoryRows(ProductDetailDto product, CancellationToken cancellationToken)
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

    private Task AddToCartAsync() => AddToCartAsync(ProductId, Math.Max(1, _quantity));

    private async Task ToggleWishlistAsync()
    {
        if (_product is null || RedirectToLoginIfAnonymous($"/products/{ProductId}"))
        {
            return;
        }

        var add = !_product.IsInWishlist;
        var ok = await TryAsync(
            () => add ? Api.AddToWishlistAsync(ProductId) : Api.RemoveFromWishlistAsync(ProductId),
            add ? "Added to your wishlist." : "Removed from your wishlist.");
        if (ok)
        {
            _product = _product with { IsInWishlist = add };
        }
    }

    private async Task SubmitReviewAsync()
    {
        _reviewError = null;
        try
        {
            await Api.AddReviewAsync(ProductId, new CreateReviewRequest(_reviewRating, _reviewTitle ?? string.Empty, _reviewComment ?? string.Empty));
        }
        catch (ApiException exception)
        {
            _reviewError = exception.Message;
            return;
        }

        _reviewTitle = null;
        _reviewComment = null;
        _reviewRating = 5;
        await ReloadReviewsAsync();
        Snackbar.Add("Thanks for your review.", Severity.Success);
    }

    private async Task DeleteReviewAsync(int reviewId)
    {
        if (await TryAsync(() => Api.DeleteReviewAsync(reviewId), "Review deleted."))
        {
            await ReloadReviewsAsync();
        }
    }

    private async Task ReloadReviewsAsync()
    {
        _reviews = await Api.GetReviewsAsync(ProductId);
        _product = await Api.GetProductAsync(ProductId);
    }

    public void Dispose()
    {
        _inventoryLoadCancellation?.Cancel();
        _inventoryLoadCancellation?.Dispose();
    }

    private sealed record InventoryGridRow(int Sequence, string ProductId, int Stock, string Availability);
}
