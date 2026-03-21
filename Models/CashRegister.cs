using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Cash register session — tracks the physical cash drawer from open to close.
/// One register per business day (#239-#243).
/// </summary>
public class CashRegister
{
    public int Id { get; set; }

    /// <summary>Opening balance entered when the register is opened.</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Actual cash counted during close (entered by cashier).</summary>
    public decimal? ClosingBalance { get; set; }

    /// <summary>System-calculated expected cash (opening + cash sales − cash returns − cash out + cash in).</summary>
    public decimal ExpectedBalance { get; set; }

    /// <summary>Difference between actual and expected (over/short).</summary>
    public decimal? Discrepancy { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Role of user who opened the register.</summary>
    [MaxLength(20)]
    public string? OpenedByRole { get; set; }

    /// <summary>Role of user who closed the register.</summary>
    [MaxLength(20)]
    public string? ClosedByRole { get; set; }

    /// <summary>Notes entered at close (e.g., "₹50 note found damaged").</summary>
    [MaxLength(500)]
    public string? CloseNotes { get; set; }

    public bool IsClosed => ClosedAt.HasValue;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<CashMovement> Movements { get; set; } = [];
}

/// <summary>
/// Manual cash-in or cash-out entry against the register (#242).
/// </summary>
public class CashMovement
{
    public int Id { get; set; }

    public int CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    /// <summary>Positive = cash in, Negative = cash out.</summary>
    public decimal Amount { get; set; }

    /// <summary>In / Out.</summary>
    [Required, MaxLength(10)]
    public string Direction { get; set; } = "In";

    [Required, MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    [MaxLength(20)]
    public string? PerformedByRole { get; set; }
}
