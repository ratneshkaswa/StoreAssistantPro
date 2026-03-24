using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// A cashier shift within a cash register day (#250).
/// Multiple shifts per day, each with own open/close and handover.
/// </summary>
public class CashRegisterShift
{
    public int Id { get; set; }

    /// <summary>Parent register for the day.</summary>
    public int CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    [MaxLength(50)]
    public string CashierRole { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? Discrepancy { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Amount handed over to next shift.</summary>
    public decimal? HandoverAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsClosed => ClosedAt.HasValue;
}
