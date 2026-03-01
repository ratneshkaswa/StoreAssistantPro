using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.Sales.ViewModels;

public partial class SaleReturnsViewModel(
    ISaleReturnService returnService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<SaleReturn> Returns { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedReturn))]
    public partial SaleReturn? SelectedReturn { get; set; }

    public bool HasSelectedReturn => SelectedReturn is not null;

    [ObservableProperty]
    public partial string CountDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    private List<SaleReturn> _allReturns = [];

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private Task LoadReturnsAsync() => RunLoadAsync(async _ =>
    {
        _allReturns = await returnService.GetAllAsync();
        ApplyFilter();
        var totalRefund = _allReturns.Sum(r => r.RefundAmount);
        CountDisplay = $"{_allReturns.Count} returns · {totalRefund:C} total refunds";
    });

    private void ApplyFilter()
    {
        var filtered = _allReturns.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.ReturnNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Reason?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        Returns = new ObservableCollection<SaleReturn>(filtered);
    }
}
