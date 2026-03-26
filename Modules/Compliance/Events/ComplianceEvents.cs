using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Compliance.Events;

/// <summary>Published when a GST return is generated.</summary>
public sealed class GstReturnGeneratedEvent(string returnType, int month, int year) : IEvent
{
    public string ReturnType { get; } = returnType;
    public int Month { get; } = month;
    public int Year { get; } = year;
}

/// <summary>Published when an e-Invoice IRN is generated.</summary>
public sealed class EInvoiceGeneratedEvent(int saleId, string irn) : IEvent
{
    public int SaleId { get; } = saleId;
    public string Irn { get; } = irn;
}

/// <summary>Published when an e-Way bill is generated.</summary>
public sealed class EWayBillGeneratedEvent(int saleId, string eWayBillNumber) : IEvent
{
    public int SaleId { get; } = saleId;
    public string EWayBillNumber { get; } = eWayBillNumber;
}
