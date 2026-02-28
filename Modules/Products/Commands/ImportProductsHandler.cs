using System.Globalization;
using System.IO;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class ImportProductsHandler(IProductService productService)
    : ICommandRequestHandler<ImportProductsCommand, ImportProductsResult>
{
    public async Task<CommandResult<ImportProductsResult>> HandleAsync(
        ImportProductsCommand command, CancellationToken ct = default)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(command.FilePath, ct).ConfigureAwait(false);
            if (lines.Length < 2)
                return CommandResult<ImportProductsResult>.Failure("File is empty or has no data rows.");

            var headers = ParseCsvLine(lines[0])
                .Select(h => h.Trim().ToUpperInvariant())
                .ToList();

            var nameIdx = headers.IndexOf("NAME");
            if (nameIdx < 0)
                return CommandResult<ImportProductsResult>.Failure("CSV must contain a 'Name' column.");

            var salePriceIdx = headers.IndexOf("SALEPRICE");
            var costPriceIdx = headers.IndexOf("COSTPRICE");
            var qtyIdx = headers.IndexOf("QUANTITY");
            var hsnIdx = headers.IndexOf("HSNCODE");
            var barcodeIdx = headers.IndexOf("BARCODE");
            var uomIdx = headers.IndexOf("UOM");
            var minStockIdx = headers.IndexOf("MINSTOCKLEVEL");

            var products = new List<Product>();
            var errors = new List<string>();
            var skipped = 0;

            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var cols = ParseCsvLine(lines[i]);
                    var name = GetCol(cols, nameIdx)?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        skipped++;
                        errors.Add($"Row {i + 1}: Name is empty, skipped.");
                        continue;
                    }

                    products.Add(new Product
                    {
                        Name = name,
                        SalePrice = ParseDecimal(GetCol(cols, salePriceIdx)),
                        CostPrice = ParseDecimal(GetCol(cols, costPriceIdx)),
                        Quantity = ParseInt(GetCol(cols, qtyIdx)),
                        HSNCode = NullIfEmpty(GetCol(cols, hsnIdx)),
                        Barcode = NullIfEmpty(GetCol(cols, barcodeIdx)),
                        UOM = GetCol(cols, uomIdx)?.Trim() is { Length: > 0 } uom ? uom : "pcs",
                        MinStockLevel = ParseInt(GetCol(cols, minStockIdx)),
                        IsActive = true
                    });
                }
                catch (Exception ex)
                {
                    skipped++;
                    errors.Add($"Row {i + 1}: {ex.Message}");
                }
            }

            if (products.Count == 0)
                return CommandResult<ImportProductsResult>.Failure("No valid products found in file.");

            var saved = await productService.AddRangeAsync(products, ct).ConfigureAwait(false);
            var result = new ImportProductsResult(saved, skipped, errors);
            return CommandResult<ImportProductsResult>.Success(result);
        }
        catch (Exception ex)
        {
            return CommandResult<ImportProductsResult>.Failure($"Import failed: {ex.Message}");
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(ch);
            }
        }

        fields.Add(field.ToString());
        return fields;
    }

    private static string? GetCol(List<string> cols, int idx) =>
        idx >= 0 && idx < cols.Count ? cols[idx] : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static int ParseInt(string? value) =>
        int.TryParse(value?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
