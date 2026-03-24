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

    /// <summary>Generate a 58mm narrow thermal receipt (#440).</summary>
    Task<string> Generate58mmReceiptAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate an A5 half-page invoice (#443).</summary>
    Task<string> GenerateA5InvoiceAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate a delivery challan — items and quantities only, no prices (#444).</summary>
    Task<string> GenerateDeliveryChallanAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate QR code payload string for a sale receipt (#129).</summary>
    Task<string> GenerateQrCodeDataAsync(int saleId, CancellationToken ct = default);

    /// <summary>Generate barcode payload string from an invoice number (#130).</summary>
    string GenerateBarcodeData(string invoiceNumber);

    /// <summary>
    /// Send a sale invoice as a PDF email attachment to the customer (#135).
    /// Returns true if the email was sent (currently a stub — future SMTP integration).
    /// </summary>
    Task<bool> SendInvoiceEmailAsync(int saleId, string recipientEmail, CancellationToken ct = default);
}
