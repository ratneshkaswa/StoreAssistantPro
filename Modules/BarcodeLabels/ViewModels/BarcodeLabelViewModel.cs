using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.BarcodeLabels.Services;

namespace StoreAssistantPro.Modules.BarcodeLabels.ViewModels;

public partial class BarcodeLabelViewModel(
    IBarcodeLabelService labelService,
    IRegionalSettingsService regional) : BaseViewModel
{
    // ── Product list ──

    [ObservableProperty]
    public partial ObservableCollection<BarcodeLabelProduct> Products { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    private IReadOnlyList<BarcodeLabelProduct> _allProducts = [];

    // ── Selection for batch printing (#382) ──

    [ObservableProperty]
    public partial ObservableCollection<LabelBatchItem> BatchItems { get; set; } = [];

    // ── Label template settings (#377) ──

    [ObservableProperty]
    public partial bool ShowProductName { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowSalePrice { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowMRP { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowBarcode { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowSizeColor { get; set; } = true;

    [ObservableProperty]
    public partial double LabelWidth { get; set; } = 38.1;

    [ObservableProperty]
    public partial double LabelHeight { get; set; } = 21.2;

    [ObservableProperty]
    public partial int ColumnsPerSheet { get; set; } = 5;

    [ObservableProperty]
    public partial int RowsPerSheet { get; set; } = 13;

    // ── Preview ──

    [ObservableProperty]
    public partial string PreviewText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    // ── Computed ──

    public int TotalLabelCount => BatchItems.Sum(b => b.Quantity);
    public int TotalSheets => ColumnsPerSheet * RowsPerSheet > 0
        ? (int)Math.Ceiling((double)TotalLabelCount / (ColumnsPerSheet * RowsPerSheet))
        : 0;
    public string TotalLabelsLabel => $"{TotalLabelCount} labels across {TotalSheets} sheet(s)";

    // ── Load ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        _allProducts = await labelService.GetProductsForLabelAsync(ct);
        FirmName = await labelService.GetFirmNameAsync(ct);
        ApplyFilter();
    });

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allProducts
            : _allProducts.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.SKU?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

        Products = new ObservableCollection<BarcodeLabelProduct>(filtered);
    }

    // ── Add to batch ──

    [RelayCommand]
    private void AddToBatch(BarcodeLabelProduct? product)
    {
        if (product is null) return;

        var existing = BatchItems.FirstOrDefault(b => b.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            BatchItems.Add(new LabelBatchItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode ?? string.Empty,
                SalePrice = regional.FormatCurrency(product.SalePrice),
                MRP = regional.FormatCurrency(product.SalePrice),
                Quantity = 1
            });
        }

        NotifyBatchTotals();
        UpdatePreview();
    }

    [RelayCommand]
    private void RemoveFromBatch(LabelBatchItem? item)
    {
        if (item is null) return;
        BatchItems.Remove(item);
        NotifyBatchTotals();
        UpdatePreview();
    }

    [RelayCommand]
    private void ClearBatch()
    {
        BatchItems.Clear();
        NotifyBatchTotals();
        UpdatePreview();
    }

    [RelayCommand]
    private void AddAllToBatch()
    {
        foreach (var product in Products)
        {
            if (BatchItems.All(b => b.ProductId != product.Id))
            {
                BatchItems.Add(new LabelBatchItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode ?? string.Empty,
                    SalePrice = regional.FormatCurrency(product.SalePrice),
                    MRP = regional.FormatCurrency(product.SalePrice),
                    Quantity = 1
                });
            }
        }

        NotifyBatchTotals();
        UpdatePreview();
    }

    // ── Print (#376, #382, #445) ──

    [RelayCommand]
    private void PrintLabels()
    {
        if (BatchItems.Count == 0)
        {
            ErrorMessage = "Add products to the batch first.";
            return;
        }

        ClearMessages();

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        var labelsPerPage = ColumnsPerSheet * RowsPerSheet;
        var allLabels = new List<LabelBatchItem>();

        foreach (var item in BatchItems)
        {
            for (var i = 0; i < item.Quantity; i++)
                allLabels.Add(item);
        }

        var doc = new FixedDocument();
        var pageWidth = printDialog.PrintableAreaWidth;
        var pageHeight = printDialog.PrintableAreaHeight;

        var cellW = pageWidth / ColumnsPerSheet;
        var cellH = pageHeight / RowsPerSheet;

        var pageIndex = 0;
        while (pageIndex < allLabels.Count)
        {
            var canvas = new Canvas { Width = pageWidth, Height = pageHeight };

            for (var row = 0; row < RowsPerSheet && pageIndex < allLabels.Count; row++)
            {
                for (var col = 0; col < ColumnsPerSheet && pageIndex < allLabels.Count; col++, pageIndex++)
                {
                    var label = allLabels[pageIndex];
                    var labelPanel = CreateLabelVisual(label, cellW, cellH);
                    Canvas.SetLeft(labelPanel, col * cellW);
                    Canvas.SetTop(labelPanel, row * cellH);
                    canvas.Children.Add(labelPanel);
                }
            }

            var page = new FixedPage { Width = pageWidth, Height = pageHeight };
            page.Children.Add(canvas);

            var content = new PageContent();
            ((System.Windows.Markup.IAddChild)content).AddChild(page);
            doc.Pages.Add(content);
        }

        printDialog.PrintDocument(doc.DocumentPaginator, "Barcode Labels");
        SuccessMessage = $"Printed {allLabels.Count} labels successfully.";
    }

    private Border CreateLabelVisual(LabelBatchItem item, double width, double height)
    {
        var content = new StackPanel();
        var panel = new Border
        {
            Width = width - 4,
            Height = height - 2,
            Margin = new Thickness(2, 1, 2, 1),
            Background = Brushes.White,
            Child = content
        };

        if (ShowProductName)
        {
            content.Children.Add(new TextBlock
            {
                Text = item.ProductName,
                FontSize = 7,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            });
        }

        if (ShowBarcode && !string.IsNullOrWhiteSpace(item.Barcode))
        {
            content.Children.Add(new TextBlock
            {
                Text = $"|||  {item.Barcode}  |||",
                FontSize = 8,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = Brushes.Black
            });

            content.Children.Add(new TextBlock
            {
                Text = item.Barcode,
                FontSize = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Black
            });
        }

        if (ShowSalePrice)
        {
            content.Children.Add(new TextBlock
            {
                Text = item.SalePrice,
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
                Foreground = Brushes.Black
            });
        }

        if (ShowMRP)
        {
            content.Children.Add(new TextBlock
            {
                Text = $"MRP: {item.MRP}",
                FontSize = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Black
            });
        }

        if (ShowSizeColor && !string.IsNullOrWhiteSpace(item.SizeColor))
        {
            content.Children.Add(new TextBlock
            {
                Text = item.SizeColor,
                FontSize = 6,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
                Foreground = Brushes.Black
            });
        }

        return panel;
    }

    // ── Preview ──

    private void UpdatePreview()
    {
        if (BatchItems.Count == 0)
        {
            PreviewText = string.Empty;
            return;
        }

        var first = BatchItems[0];
        var lines = new List<string>();

        if (ShowProductName) lines.Add(first.ProductName);
        if (ShowBarcode && !string.IsNullOrEmpty(first.Barcode))
        {
            lines.Add($"|||  {first.Barcode}  |||");
            lines.Add(first.Barcode);
        }
        if (ShowSalePrice) lines.Add(first.SalePrice);
        if (ShowMRP) lines.Add($"MRP: {first.MRP}");
        if (ShowSizeColor && !string.IsNullOrEmpty(first.SizeColor))
            lines.Add(first.SizeColor);

        PreviewText = string.Join(Environment.NewLine, lines);
    }

    private void NotifyBatchTotals()
    {
        OnPropertyChanged(nameof(TotalLabelCount));
        OnPropertyChanged(nameof(TotalSheets));
        OnPropertyChanged(nameof(TotalLabelsLabel));
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    partial void OnShowProductNameChanged(bool value) => UpdatePreview();
    partial void OnShowSalePriceChanged(bool value) => UpdatePreview();
    partial void OnShowMRPChanged(bool value) => UpdatePreview();
    partial void OnShowBarcodeChanged(bool value) => UpdatePreview();
    partial void OnShowSizeColorChanged(bool value) => UpdatePreview();
    partial void OnColumnsPerSheetChanged(int value) => NotifyBatchTotals();
    partial void OnRowsPerSheetChanged(int value) => NotifyBatchTotals();

    // ── Auto-generate barcode (#384) ──

    [RelayCommand]
    private Task AutoGenerateBarcodeAsync(BarcodeLabelProduct? product) => RunAsync(async ct =>
    {
        if (product is null) return;
        ClearMessages();

        if (!string.IsNullOrWhiteSpace(product.Barcode))
        {
            ErrorMessage = $"{product.Name} already has barcode: {product.Barcode}";
            return;
        }

        var barcode = await labelService.AutoGenerateBarcodeAsync(product.Id, ct);
        SuccessMessage = $"Generated barcode {barcode} for {product.Name}.";

        // Refresh product list
        _allProducts = await labelService.GetProductsForLabelAsync(ct);
        ApplyFilter();
    });

    // ── Add variant labels (#381) ──

    [RelayCommand]
    private Task AddVariantsToBatchAsync(BarcodeLabelProduct? product) => RunAsync(async ct =>
    {
        if (product is null) return;
        ClearMessages();

        var variants = await labelService.GetVariantsForProductAsync(product.Id, ct);
        if (variants.Count == 0)
        {
            ErrorMessage = $"{product.Name} has no active variants.";
            return;
        }

        foreach (var v in variants)
        {
            var sizeColor = string.Join(" / ",
                new[] { v.SizeName, v.ColorName }.Where(s => !string.IsNullOrEmpty(s)));
            var displayName = string.IsNullOrEmpty(sizeColor)
                ? v.ProductName
                : $"{v.ProductName} ({sizeColor})";

            if (BatchItems.All(b => b.ProductId != v.VariantId * -1))
            {
                BatchItems.Add(new LabelBatchItem
                {
                    ProductId = v.VariantId * -1,
                    ProductName = displayName,
                    Barcode = v.Barcode ?? string.Empty,
                    SalePrice = regional.FormatCurrency(product.SalePrice + v.AdditionalPrice),
                    MRP = regional.FormatCurrency(product.SalePrice + v.AdditionalPrice),
                    SizeColor = sizeColor,
                    Quantity = 1
                });
            }
        }

        SuccessMessage = $"Added {variants.Count} variant label(s) for {product.Name}.";
        NotifyBatchTotals();
        UpdatePreview();
    });
}

/// <summary>A product+quantity entry in the batch print queue.</summary>
public partial class LabelBatchItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SalePrice { get; set; } = string.Empty;
    public string MRP { get; set; } = string.Empty;
    public string SizeColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Quantity { get; set; } = 1;
}
