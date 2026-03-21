using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public class CashRegisterService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAuditService auditService,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : ICashRegisterService
{
    public async Task<CashRegister> OpenRegisterAsync(
        decimal openingBalance, string cashierRole, CancellationToken ct = default)
    {
        if (openingBalance < 0)
            throw new InvalidOperationException("Opening balance cannot be negative.");

        using var scope = perf.BeginScope("CashRegisterService.OpenRegisterAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.CashRegisters
            .FirstOrDefaultAsync(r => r.ClosedAt == null, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            throw new InvalidOperationException("A register is already open. Close it before opening a new one.");

        var register = new CashRegister
        {
            OpeningBalance = openingBalance,
            ExpectedBalance = openingBalance,
            OpenedAt = regional.Now,
            OpenedByRole = cashierRole
        };

        context.CashRegisters.Add(register);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        _ = auditService.LogAsync("RegisterOpened", "CashRegister", register.Id.ToString(),
            null, $"Opening={openingBalance}", cashierRole, null, ct);

        return register;
    }

    public async Task<CashRegister> CloseRegisterAsync(
        int registerId, decimal closingBalance, string? notes,
        string cashierRole, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("CashRegisterService.CloseRegisterAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var register = await context.CashRegisters
            .FirstOrDefaultAsync(r => r.Id == registerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Register Id {registerId} not found.");

        if (register.IsClosed)
            throw new InvalidOperationException("This register is already closed.");

        var expected = await CalculateExpectedBalanceInternalAsync(context, registerId, register.OpeningBalance, register.OpenedAt, ct);

        register.ExpectedBalance = expected;
        register.ClosingBalance = closingBalance;
        register.Discrepancy = closingBalance - expected;
        register.ClosedAt = regional.Now;
        register.ClosedByRole = cashierRole;
        register.CloseNotes = notes;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        // Audit: register closed (#254)
        _ = auditService.LogAsync("RegisterClosed", "CashRegister", registerId.ToString(),
            $"Expected={expected}", $"Actual={closingBalance}", cashierRole,
            $"Discrepancy={register.Discrepancy}", ct);

        // Cash variance alert (#252) — audit warning when discrepancy is significant
        if (Math.Abs(register.Discrepancy ?? 0) > 100)
            _ = auditService.LogAsync("CashVarianceAlert", "CashRegister", registerId.ToString(),
                $"Expected={expected}", $"Actual={closingBalance}", cashierRole,
                $"Variance={register.Discrepancy} exceeds threshold", ct);

        return register;
    }

    public async Task<CashMovement> RecordMovementAsync(
        int registerId, decimal amount, string direction, string reason,
        string performedByRole, CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Movement amount must be positive.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Reason is required for cash movements.");

        using var scope = perf.BeginScope("CashRegisterService.RecordMovementAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var register = await context.CashRegisters
            .FirstOrDefaultAsync(r => r.Id == registerId && r.ClosedAt == null, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("No open register found.");

        var movement = new CashMovement
        {
            CashRegisterId = registerId,
            Amount = string.Equals(direction, "Out", StringComparison.OrdinalIgnoreCase) ? -amount : amount,
            Direction = direction,
            Reason = reason,
            Timestamp = regional.Now,
            PerformedByRole = performedByRole
        };

        context.CashMovements.Add(movement);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        _ = auditService.LogAsync($"CashMovement{direction}", "CashMovement", movement.Id.ToString(),
            null, $"{direction} {amount}", performedByRole, reason, ct);

        return movement;
    }

    public async Task<CashRegister?> GetOpenRegisterAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CashRegisterService.GetOpenRegisterAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.CashRegisters
            .AsNoTracking()
            .Include(r => r.Movements)
            .FirstOrDefaultAsync(r => r.ClosedAt == null, ct)
            .ConfigureAwait(false);
    }

    public async Task<decimal> CalculateExpectedBalanceAsync(int registerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CashRegisterService.CalculateExpectedBalanceAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var register = await context.CashRegisters
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == registerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Register Id {registerId} not found.");

        return await CalculateExpectedBalanceInternalAsync(
            context, registerId, register.OpeningBalance, register.OpenedAt, ct);
    }

    public async Task<IReadOnlyList<CashRegister>> GetRegisterHistoryAsync(int count = 30, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CashRegisterService.GetRegisterHistoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.CashRegisters
            .AsNoTracking()
            .Where(r => r.ClosedAt != null)
            .OrderByDescending(r => r.ClosedAt)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<DayEndSummary> GetDayEndSummaryAsync(int registerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CashRegisterService.GetDayEndSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var register = await context.CashRegisters
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == registerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Register Id {registerId} not found.");

        var from = register.OpenedAt;
        var to = register.ClosedAt ?? regional.Now;

        // Sales within this register session
        var sales = await context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var allPayments = sales.SelectMany(s => s.Payments).ToList();

        var totalCash = allPayments
            .Where(p => string.Equals(p.Method, "Cash", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);
        var totalCard = allPayments
            .Where(p => string.Equals(p.Method, "Card", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);
        var totalUpi = allPayments
            .Where(p => string.Equals(p.Method, "UPI", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);
        var totalCredit = allPayments
            .Where(p => string.Equals(p.Method, "Credit", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);

        // Returns (cash refunds)
        var returns = await context.SaleReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= from && r.ReturnDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var totalCashReturns = returns.Sum(r => r.RefundAmount);

        // Cash movements
        var movements = await context.CashMovements
            .AsNoTracking()
            .Where(m => m.CashRegisterId == registerId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var totalCashIn = movements.Where(m => m.Amount > 0).Sum(m => m.Amount);
        var totalCashOut = movements.Where(m => m.Amount < 0).Sum(m => Math.Abs(m.Amount));

        var expected = register.OpeningBalance + totalCash - totalCashReturns + totalCashIn - totalCashOut;

        return new DayEndSummary(
            registerId,
            register.OpenedAt,
            register.ClosedAt,
            register.OpeningBalance,
            totalCash,
            totalCard,
            totalUpi,
            totalCredit,
            totalCashReturns,
            totalCashIn,
            totalCashOut,
            expected,
            register.ClosingBalance,
            register.Discrepancy,
            sales.Count,
            returns.Count);
    }

    private async Task<decimal> CalculateExpectedBalanceInternalAsync(
        AppDbContext context, int registerId, decimal openingBalance,
        DateTime openedAt, CancellationToken ct)
    {
        // Cash from sales
        var cashFromSales = await context.SalePayments
            .Where(p => p.Sale!.SaleDate >= openedAt
                && string.Equals(p.Method, "Cash", StringComparison.CurrentCultureIgnoreCase))
            .SumAsync(p => p.Amount, ct)
            .ConfigureAwait(false);

        // Cash returns
        var cashReturns = await context.SaleReturns
            .Where(r => r.ReturnDate >= openedAt)
            .SumAsync(r => r.RefundAmount, ct)
            .ConfigureAwait(false);

        // Manual movements
        var movements = await context.CashMovements
            .Where(m => m.CashRegisterId == registerId)
            .SumAsync(m => m.Amount, ct)
            .ConfigureAwait(false);

        return openingBalance + cashFromSales - cashReturns + movements;
    }

    public async Task<bool> IsDayClosedAsync(DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CashRegisterService.IsDayClosedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await context.CashRegisters
            .AnyAsync(r => r.OpenedAt >= dayStart && r.OpenedAt < dayEnd && r.ClosedAt != null, ct)
            .ConfigureAwait(false);
    }
}
