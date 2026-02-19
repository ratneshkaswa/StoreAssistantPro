using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Models;
using StoreAssistantPro.Services;

namespace StoreAssistantPro.ViewModels;

public partial class ProductsViewModel(IProductService productService) : ObservableObject
{
    private List<Product> _allProducts = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

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

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter(value);
    }

    private void ApplyFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            Products = new ObservableCollection<Product>(_allProducts);
        }
        else
        {
            var filtered = _allProducts
                .Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Products = new ObservableCollection<Product>(filtered);
        }
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var items = await productService.GetAllAsync();
        _allProducts = items.ToList();
        ApplyFilter(SearchText);
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        NewProductName = string.Empty;
        NewProductPrice = 0;
        NewProductQuantity = 0;
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
        if (string.IsNullOrWhiteSpace(NewProductName)) return;
        if (NewProductPrice < 0) throw new InvalidOperationException("Price cannot be negative.");
        if (NewProductQuantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");

        var product = new Product
        {
            Name = NewProductName.Trim(),
            Price = NewProductPrice,
            Quantity = NewProductQuantity
        };

        await productService.AddAsync(product);
        _allProducts.Add(product);
        ApplyFilter(SearchText);

        IsAddFormVisible = false;
    }

    [RelayCommand]
    private void ShowEditForm()
    {
        if (SelectedProduct is null) return;

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
        if (SelectedProduct is null || string.IsNullOrWhiteSpace(EditProductName)) return;
        if (EditProductPrice < 0) throw new InvalidOperationException("Price cannot be negative.");
        if (EditProductQuantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");

        SelectedProduct.Name = EditProductName.Trim();
        SelectedProduct.Price = EditProductPrice;
        SelectedProduct.Quantity = EditProductQuantity;

        await productService.UpdateAsync(SelectedProduct);

        IsEditFormVisible = false;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct is null) return;

        await productService.DeleteAsync(SelectedProduct.Id);
        _allProducts.Remove(SelectedProduct);
        Products.Remove(SelectedProduct);
        SelectedProduct = null;
    }
}
