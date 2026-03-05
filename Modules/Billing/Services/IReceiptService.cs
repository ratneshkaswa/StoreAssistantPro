using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface IReceiptService
{
    /// <summary>Generate a plain-text 80mm thermal receipt for a sale.</summary>
    Task<string> GenerateThermalReceiptAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate receipt text from a Sale object already in memory.</summary>
    string GenerateThermalReceipt(Sale sale, string firmName, string firmAddress, string firmPhone, string firmGSTIN, string footerText);
}
