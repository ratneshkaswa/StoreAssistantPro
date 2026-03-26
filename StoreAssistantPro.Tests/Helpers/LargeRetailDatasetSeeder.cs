using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

internal static class LargeRetailDatasetSeeder
{
    public static async Task<LargeRetailDatasetSummary> SeedAsync(
        AppDbContext context,
        int productCount = 500,
        int saleCount = 750,
        int itemsPerSale = 3)
    {
        var random = new Random(42042);
        var startDate = new DateTime(2026, 3, 1, 9, 0, 0);

        var products = Enumerable.Range(1, productCount)
            .Select(index => new Product
            {
                Name = $"Load Product {index:D4}",
                SalePrice = 100m + index,
                CostPrice = 60m + index / 2m,
                Quantity = 500,
                IsActive = true,
                MinStockLevel = 5
            })
            .ToList();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var sales = new List<Sale>(saleCount);
        var saleItems = new List<SaleItem>(saleCount * itemsPerSale);

        for (var saleIndex = 0; saleIndex < saleCount; saleIndex++)
        {
            var saleDate = startDate.AddMinutes(saleIndex * 7);
            var paymentMethod = (saleIndex % 3) switch
            {
                0 => "Cash",
                1 => "UPI",
                _ => "Card"
            };

            var sale = new Sale
            {
                InvoiceNumber = $"LD-{saleDate:yyyyMMdd}-{saleIndex + 1:D4}",
                SaleDate = saleDate,
                TotalAmount = 0,
                PaymentMethod = paymentMethod,
                IdempotencyKey = Guid.NewGuid(),
                CashierRole = saleIndex % 2 == 0 ? "Admin" : "Cashier"
            };

            sales.Add(sale);
        }

        context.Sales.AddRange(sales);
        await context.SaveChangesAsync();

        for (var saleIndex = 0; saleIndex < saleCount; saleIndex++)
        {
            var sale = sales[saleIndex];
            decimal total = 0;

            for (var itemIndex = 0; itemIndex < itemsPerSale; itemIndex++)
            {
                var product = products[(saleIndex * itemsPerSale + itemIndex * 11) % products.Count];
                var quantity = random.Next(1, 4);
                var unitPrice = product.SalePrice;
                var discountRate = itemIndex == 0 ? 5m : 0m;
                var lineSubtotal = quantity * unitPrice * (1 - discountRate / 100m);
                var taxAmount = Math.Round(lineSubtotal * 0.05m, 2);

                total += lineSubtotal;
                product.Quantity -= quantity;

                saleItems.Add(new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    ItemDiscountRate = discountRate,
                    ItemFlatDiscount = 0,
                    TaxRate = 5,
                    TaxAmount = taxAmount,
                    IsTaxInclusive = false
                });
            }

            sale.TotalAmount = total;
        }

        context.SaleItems.AddRange(saleItems);
        await context.SaveChangesAsync();

        return new LargeRetailDatasetSummary(products.Count, sales.Count, saleItems.Count);
    }
}

internal sealed record LargeRetailDatasetSummary(
    int ProductCount,
    int SaleCount,
    int SaleItemCount);
