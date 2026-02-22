using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductsViewModel(
    IProductService productService,
    ISessionService sessionService,
    IDialogService dialogService,
    IMasterPinValidator masterPinValidator,
    ICommandBus commandBus) : BaseViewModel
{
    private const int PageSize = 50;
    private CancellationTokenSource? _searchCts;

    // ── Role-based access ──

    public bool CanManageProducts =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;

    public bool CanDeleteProducts =>
        sessionService.CurrentUserType is UserType.Admin;

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    // ── Paging state ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int TotalPages { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    public partial int TotalCount { get; set; }

    public string PageDisplay => TotalPages > 0
        ? $"Page {PageIndex + 1} of {TotalPages} ({TotalCount} items)"
        : "No results";

    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;

    // ── Add form ──

    [ObservableProperty]
    public partial string NewProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal NewProductPrice { get; set; }

    [ObservableProperty]
    public partial int NewProductQuantity { get; set; }

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditFormVisible { get; set; }

    [ObservableProperty]
    public partial string EditProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal EditProductPrice { get; set; }

    [ObservableProperty]
    public partial int EditProductQuantity { get; set; }

    // ── Search (server-side with debounce) ──

    partial void OnSearchTextChanged(string value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    private async void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            await Task.Delay(300, ct);
            await LoadProductsAsync();
        }
        catch (OperationCanceledException)
        {
            // Debounce — superseded by newer keystroke.
        }
    }

    // ── Data loading ──

    [RelayCommand]
    private Task LoadProductsAsync() => RunLoadAsync(async ct =>
    {
        var result = await productService.GetPagedAsync(
            new PagedQuery(PageIndex, PageSize, SearchText), ct);

        Products = new ObservableCollection<Product>(result.Items);
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        PageIndex = result.PageIndex;
    });

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private Task NextPageAsync()
    {
        PageIndex++;
        return LoadProductsAsync();
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private Task PreviousPageAsync()
    {
        PageIndex--;
        return LoadProductsAsync();
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can add products.";
            return;
        }

        ErrorMessage = string.Empty;
        NewProductName = string.Empty;
        NewProductPrice = 0;
        NewProductQuantity = 1;
        IsEditFormVisible = false;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(NewProductName), "Product name is required.")
            .Rule(InputValidator.IsNonNegative(NewProductPrice), "Price cannot be negative.")
            .Rule(InputValidator.IsNonNegative(NewProductQuantity), "Quantity cannot be negative.")))
            return;

        var result = await commandBus.SendAsync(new SaveProductCommand(
            NewProductName.Trim(), NewProductPrice, NewProductQuantity));

        if (result.Succeeded)
        {
            IsAddFormVisible = false;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Save failed.";
        }
    }

    [RelayCommand]
    private void ShowEditForm()
    {
        if (SelectedProduct is null) return;

        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can edit products.";
            return;
        }

        ErrorMessage = string.Empty;
        EditProductName = SelectedProduct.Name;
        EditProductPrice = SelectedProduct.Price;
        EditProductQuantity = SelectedProduct.Quantity;
        IsAddFormVisible = false;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (!Validate(v => v
            .Rule(SelectedProduct is not null, "No product selected.")
            .Rule(InputValidator.IsRequired(EditProductName), "Product name is required.")
            .Rule(InputValidator.IsNonNegative(EditProductPrice), "Price cannot be negative.")
            .Rule(InputValidator.IsNonNegative(EditProductQuantity), "Quantity cannot be negative.")))
            return;

        var result = await commandBus.SendAsync(new UpdateProductCommand(
            SelectedProduct.Id, EditProductName.Trim(), EditProductPrice,
            EditProductQuantity, SelectedProduct.RowVersion));

        if (result.Succeeded)
        {
            IsEditFormVisible = false;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Update failed.";
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedProduct is null) return;

        if (!CanDeleteProducts)
        {
            ErrorMessage = "Only administrators can delete products.";
            return;
        }

        if (!dialogService.Confirm(
            $"Delete '{SelectedProduct.Name}'?\n\nThis action cannot be undone.",
            "Delete Product"))
            return;

        if (!await masterPinValidator.ValidateAsync("Enter Master PIN to delete this product."))
        {
            ErrorMessage = "Master PIN validation failed. Delete cancelled.";
            return;
        }

        var result = await commandBus.SendAsync(
            new DeleteProductCommand(SelectedProduct.Id, SelectedProduct.RowVersion));

        if (result.Succeeded)
        {
            SelectedProduct = null;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Delete failed.";
            await LoadProductsAsync();
        }
    }
}
