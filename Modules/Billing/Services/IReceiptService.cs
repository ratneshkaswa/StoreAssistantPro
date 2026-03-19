using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface IReceiptService
{
    /// <summary>Generate a plain-text 80mm thermal receipt for a sale.</summary>
    Task<string> GenerateThermalReceiptAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate receipt text from a Sale object already in memory.</summary>
    string GenerateThermalReceipt(Sale sale, string firmName, string firmAddress, string firmPhone, string firmGSTIN, string footerText);

    /// <summary>Generate a plain-text 80mm return/credit note receipt.</summary>
    Task<string> GenerateReturnReceiptAsync(int returnId, CancellationToken ct = default);

    /// <summary>Generate a full A4 GST tax invoice with buyer details (#131/#134).</summary>
    Task<string> GenerateA4InvoiceAsync(int saleId, CancellationToken ct = default);
}
