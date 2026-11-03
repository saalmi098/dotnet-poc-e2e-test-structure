using System.ComponentModel.DataAnnotations;

namespace ShopDemo.Client.Pages;

public partial class ManageCoupons
{
    private IReadOnlyList<CouponDto> _coupons = [];
    private readonly CouponModel _model = new();
    private string? _loadError;
    private string? _error;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (RedirectUnlessManager("/manage/coupons"))
        {
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _coupons = await Api.GetCouponsAsync();
            _loadError = null;
        }
        catch (ApiException exception)
        {
            _loadError = $"Coupons could not be loaded. {exception.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private static string Describe(CouponDto coupon) => coupon.Type switch
    {
        CouponType.Percentage => $"{coupon.Value:0.##}% off",
        CouponType.FixedAmount => $"{coupon.Value.ToMoney()} off",
        _ => "Free shipping"
    };

    private async Task CreateAsync()
    {
        _error = null;
        try
        {
            var expiresAt = _model.ExpiresOn is { } date
                ? new DateTimeOffset(date.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.Local.GetUtcOffset(date))
                : (DateTimeOffset?)null;
            await Api.CreateCouponAsync(new SaveCouponRequest(
                _model.Code, _model.Description ?? string.Empty, _model.Type, _model.Value,
                _model.MinSubtotal, expiresAt, _model.MaxRedemptions));
            Snackbar.Add($"Coupon {_model.Code.ToUpperInvariant()} created.", Severity.Success);
            _model.Reset();
            await LoadAsync();
        }
        catch (ApiException exception)
        {
            _error = exception.Message;
        }
    }

    private async Task ToggleAsync(CouponDto coupon, bool active)
    {
        if (await TryAsync(() => Api.UpdateCouponAsync(new SaveCouponRequest(
                coupon.Code, coupon.Description, coupon.Type, coupon.Value, coupon.MinSubtotal,
                coupon.ExpiresAt, coupon.MaxRedemptions, active))))
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync(CouponDto coupon)
    {
        if (await TryAsync(() => Api.DeleteCouponAsync(coupon.Code), $"Coupon {coupon.Code} deleted."))
        {
            await LoadAsync();
        }
    }

    private sealed class CouponModel
    {
        [Required(ErrorMessage = "Enter a coupon code."), RegularExpression("^[A-Za-z0-9]{3,40}$", ErrorMessage = "The code must be 3-40 letters or digits.")]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }
        public CouponType Type { get; set; } = CouponType.Percentage;
        public decimal Value { get; set; }
        public decimal MinSubtotal { get; set; }
        public int? MaxRedemptions { get; set; }
        public DateTime? ExpiresOn { get; set; }

        public void Reset()
        {
            Code = string.Empty;
            Description = null;
            Type = CouponType.Percentage;
            Value = 0;
            MinSubtotal = 0;
            MaxRedemptions = null;
            ExpiresOn = null;
        }
    }
}
