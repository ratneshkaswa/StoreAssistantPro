using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

public partial class SaleHistoryViewModel(
    ISaleHistoryService historyService,
    IReceiptService receiptService) : BaseViewModel
{
    // ── Filters ──

    [ObservableProperty]
    public partial DateTime? DateFrom { get; set; }

    [ObservableProperty]
    public partial DateTime? DateTo { get; set; }

    [ObservableProperty]
    public partial string InvoiceSearch { get; set; } = string.Empty;

    // ── Data ──

    [ObservableProperty]
    public partial ObservableCollection<Sale> Sales { get; set; } = [];

    [ObservableProperty]
    public partial Sale? SelectedSale { get; set; }

    [ObservableProperty]
    public partial string SaleDetail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReceiptPreview { get; set; } = string.Empty;

    partial void OnSelectedSaleChanged(Sale? value)
    {
        if (value is null)
        {
            SaleDetail = string.Empty;
            ReceiptPreview = string.Empty;
            return;
        }

        SaleDetail = $"Invoice: {value.InvoiceNumber}\n" +
                     $"Date: {value.SaleDate:dd-MMM-yyyy HH:mm}\n" +
                     $"Items: {value.Items.Count}\n" +
                     $"Total: ₹{value.TotalAmount:N2}\n" +
                     $"Payment: {value.PaymentMethod}" +
                     (string.IsNullOrEmpty(value.PaymentReference) ? "" : $" ({value.PaymentReference})") +
                     (value.DiscountAmount > 0 ? $"\nDiscount: ₹{value.DiscountAmount:N2}" : "");
    }

    // ── Commands ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var sales = await historyService.GetSalesAsync(DateFrom, DateTo, InvoiceSearch, ct);
        Sales = new ObservableCollection<Sale>(sales);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        var sales = await historyService.GetSalesAsync(DateFrom, DateTo, InvoiceSearch, ct);
        Sales = new ObservableCollection<Sale>(sales);
        SuccessMessage = $"{sales.Count} sale(s) found.";
    });

    [RelayCommand]
    private Task PreviewReceiptAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        if (SelectedSale is null)
        {
            ErrorMessage = "Select a sale first.";
            return;
        }

        var receipt = await receiptService.GenerateThermalReceiptAsync(SelectedSale.Id, ct);
        ReceiptPreview = receipt;
    });
}
