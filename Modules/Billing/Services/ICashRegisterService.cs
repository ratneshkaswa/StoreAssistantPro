using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Cash register / cash drawer management (#239-#243).
/// </summary>
public interface ICashRegisterService
{
    /// <summary>Open the register with an opening balance (#240).</summary>
    Task<CashRegister> OpenRegisterAsync(decimal openingBalance, string cashierRole, CancellationToken ct = default);

    /// <summary>Close the register with actual counted cash (#241).</summary>
    Task<CashRegister> CloseRegisterAsync(int registerId, decimal closingBalance, string? notes, string cashierRole, CancellationToken ct = default);

    /// <summary>Record a manual cash in or cash out (#242).</summary>
    Task<CashMovement> RecordMovementAsync(int registerId, decimal amount, string direction, string reason, string performedByRole, CancellationToken ct = default);

    /// <summary>Get the currently open register (if any).</summary>
    Task<CashRegister?> GetOpenRegisterAsync(CancellationToken ct = default);

    /// <summary>Calculate the expected cash balance for a register (#243).</summary>
    Task<decimal> CalculateExpectedBalanceAsync(int registerId, CancellationToken ct = default);

    /// <summary>Get register history (closed registers).</summary>
    Task<IReadOnlyList<CashRegister>> GetRegisterHistoryAsync(int count = 30, CancellationToken ct = default);

    /// <summary>Get day-end summary for the register (#241).</summary>
    Task<DayEndSummary> GetDayEndSummaryAsync(int registerId, CancellationToken ct = default);

    /// <summary>Check if the business day is closed — no transactions allowed after close (#246).</summary>
    Task<bool> IsDayClosedAsync(DateTime date, CancellationToken ct = default);

    // ── Shift support (#250) ──

    /// <summary>Open a new shift within the current register.</summary>
    Task<CashRegisterShift> OpenShiftAsync(int registerId, decimal openingBalance, string cashierRole, CancellationToken ct = default);

    /// <summary>Close the current shift with handover amount.</summary>
    Task<CashRegisterShift> CloseShiftAsync(int shiftId, decimal closingBalance, decimal? handoverAmount, string? notes, CancellationToken ct = default);

    /// <summary>Get shifts for a register.</summary>
    Task<IReadOnlyList<CashRegisterShift>> GetShiftsAsync(int registerId, CancellationToken ct = default);

    /// <summary>Get the currently open shift.</summary>
    Task<CashRegisterShift?> GetOpenShiftAsync(int registerId, CancellationToken ct = default);

    // ── Cash register approval (#253) ──

    /// <summary>Approve a day close (manager sign-off).</summary>
    Task ApproveCloseAsync(int registerId, string approverRole, CancellationToken ct = default);
}

public record DayEndSummary(
    int RegisterId,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningBalance,
    decimal TotalCashSales,
    decimal TotalCardSales,
    decimal TotalUpiSales,
    decimal TotalCreditSales,
    decimal TotalCashReturns,
    decimal TotalCashIn,
    decimal TotalCashOut,
    decimal ExpectedCash,
    decimal? ActualCash,
    decimal? Discrepancy,
    int SaleCount,
    int ReturnCount);
