using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Promotions.Commands;
using StoreAssistantPro.Modules.Promotions.Services;

namespace StoreAssistantPro.Modules.Promotions.ViewModels;

public partial class PromotionsViewModel(
    ICouponService couponService,
    IVoucherService voucherService,
    ICommandBus commandBus) : BaseViewModel
{
    // ── Coupons ──

    [ObservableProperty]
    public partial ObservableCollection<Coupon> Coupons { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCoupon))]
    public partial Coupon? SelectedCoupon { get; set; }

    public bool HasSelectedCoupon => SelectedCoupon is not null;

    // ── Vouchers ──

    [ObservableProperty]
    public partial ObservableCollection<Voucher> Vouchers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedVoucher))]
    public partial Voucher? SelectedVoucher { get; set; }

    public bool HasSelectedVoucher => SelectedVoucher is not null;

    [ObservableProperty]
    public partial string CountDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    // ── Add Coupon form ──

    [ObservableProperty]
    public partial bool IsAddCouponVisible { get; set; }

    [ObservableProperty]
    public partial string NewCouponCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewCouponDiscountValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewCouponMinBill { get; set; } = string.Empty;

    // ── Add Voucher form ──

    [ObservableProperty]
    public partial bool IsAddVoucherVisible { get; set; }

    [ObservableProperty]
    public partial string NewVoucherCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewVoucherFaceValue { get; set; } = string.Empty;

    [RelayCommand]
    private Task LoadPromotionsAsync() => RunLoadAsync(async _ =>
    {
        var coupons = await couponService.GetAllAsync();
        Coupons = new ObservableCollection<Coupon>(coupons);

        var vouchers = await voucherService.GetAllAsync();
        Vouchers = new ObservableCollection<Voucher>(vouchers);

        CountDisplay = $"{coupons.Count} coupons · {vouchers.Count} vouchers";
    });

    // ── Coupon CRUD ──

    [RelayCommand]
    private void ShowAddCoupon()
    {
        IsAddCouponVisible = true;
        NewCouponCode = NewCouponDiscountValue = NewCouponMinBill = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelAddCoupon() => IsAddCouponVisible = false;

    [RelayCommand]
    private async Task SaveCouponAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCouponCode))
        {
            ErrorMessage = "Code is required.";
            return;
        }
        if (!decimal.TryParse(NewCouponDiscountValue, out var discountVal) || discountVal <= 0)
        {
            ErrorMessage = "Valid discount value is required.";
            return;
        }

        var coupon = new Coupon
        {
            Code = NewCouponCode.Trim().ToUpperInvariant(),
            DiscountType = DiscountType.Percentage,
            DiscountValue = discountVal,
            MinBillAmount = decimal.TryParse(NewCouponMinBill, out var min) ? min : 0m,
            IsActive = true
        };

        var result = await commandBus.SendAsync(new SaveCouponCommand(coupon));
        if (result.Succeeded)
        {
            IsAddCouponVisible = false;
            SuccessMessage = $"Coupon '{coupon.Code}' created.";
            await LoadPromotionsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to save coupon.";
        }
    }

    [RelayCommand]
    private async Task DeleteCouponAsync()
    {
        if (SelectedCoupon is null) return;
        var result = await commandBus.SendAsync(new DeleteCouponCommand(SelectedCoupon.Id));
        if (result.Succeeded)
        {
            SuccessMessage = $"Coupon '{SelectedCoupon.Code}' deleted.";
            await LoadPromotionsAsync();
        }
    }

    // ── Voucher CRUD ──

    [RelayCommand]
    private void ShowAddVoucher()
    {
        IsAddVoucherVisible = true;
        NewVoucherCode = NewVoucherFaceValue = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelAddVoucher() => IsAddVoucherVisible = false;

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVoucherCode))
        {
            ErrorMessage = "Code is required.";
            return;
        }
        if (!decimal.TryParse(NewVoucherFaceValue, out var faceVal) || faceVal <= 0)
        {
            ErrorMessage = "Valid face value is required.";
            return;
        }

        var voucher = new Voucher
        {
            Code = NewVoucherCode.Trim().ToUpperInvariant(),
            FaceValue = faceVal,
            Balance = faceVal,
            IsActive = true
        };

        var result = await commandBus.SendAsync(new SaveVoucherCommand(voucher));
        if (result.Succeeded)
        {
            IsAddVoucherVisible = false;
            SuccessMessage = $"Voucher '{voucher.Code}' created with ₹{faceVal}.";
            await LoadPromotionsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to save voucher.";
        }
    }

    [RelayCommand]
    private async Task DeleteVoucherAsync()
    {
        if (SelectedVoucher is null) return;
        var result = await commandBus.SendAsync(new DeleteVoucherCommand(SelectedVoucher.Id));
        if (result.Succeeded)
        {
            SuccessMessage = $"Voucher '{SelectedVoucher.Code}' deleted.";
            await LoadPromotionsAsync();
        }
    }
}
