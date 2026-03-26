using StoreAssistantPro.Models.Compliance;

namespace StoreAssistantPro.Modules.Compliance.Services;

/// <summary>GST return generation service (#832-840).</summary>
public interface IGstReturnService
{
    Task<GstReturnData> GenerateGstr1Async(int month, int year, CancellationToken ct = default);
    Task<GstReturnData> GenerateGstr3bAsync(int month, int year, CancellationToken ct = default);
    Task<GstReturnData> GenerateGstr9Async(int year, CancellationToken ct = default);
    Task<IReadOnlyList<HsnSummaryEntry>> GetHsnSummaryAsync(int month, int year, CancellationToken ct = default);
}

/// <summary>e-Way bill service (#835).</summary>
public interface IEWayBillService
{
    Task<EWayBill> GenerateAsync(int saleId, string transportMode, string? vehicleNumber = null, CancellationToken ct = default);
    Task<EWayBill?> GetBySaleAsync(int saleId, CancellationToken ct = default);
    Task<bool> ValidateAsync(string ewayBillNumber, CancellationToken ct = default);
}

/// <summary>e-Invoice service with IRN (#836-837).</summary>
public interface IEInvoiceService
{
    Task<EInvoice> GenerateAsync(int saleId, CancellationToken ct = default);
    Task<EInvoice?> GetBySaleAsync(int saleId, CancellationToken ct = default);
    Task<string> GenerateQrCodeAsync(int saleId, CancellationToken ct = default);
}

/// <summary>Data compliance service (#842-848).</summary>
public interface IDataComplianceService
{
    Task<IReadOnlyList<RetentionPolicy>> GetPoliciesAsync(CancellationToken ct = default);
    Task SavePolicyAsync(RetentionPolicy policy, CancellationToken ct = default);
    Task<int> PurgeExpiredDataAsync(CancellationToken ct = default);
    Task DeleteCustomerDataAsync(int customerId, CancellationToken ct = default);
}
