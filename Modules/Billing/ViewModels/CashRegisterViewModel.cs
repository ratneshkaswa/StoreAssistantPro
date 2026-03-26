using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Backup.Services;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

public partial class CashRegisterViewModel(
    ICashRegisterService cashRegisterService,
    IBackupService backupService,
    ILogger<CashRegisterViewModel> logger,
    IAppStateService appState,
    IRegionalSettingsService regional) : BaseViewModel
{
    // ── Register state ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegisterOpen))]
    [NotifyPropertyChangedFor(nameof(IsRegisterClosed))]
    public partial CashRegister? OpenRegister { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayEndSummary))]
    [NotifyPropertyChangedFor(nameof(HasCashVarianceAlert))]
    [NotifyPropertyChangedFor(nameof(CashVarianceAlertMessage))]
    public partial DayEndSummary? DayEndSummary { get; set; }

    public bool HasDayEndSummary => DayEndSummary is not null;

    /// <summary>Variance threshold (₹) above which a warning is shown (#252).</summary>
    private const decimal VarianceAlertThreshold = 100m;

    public bool HasCashVarianceAlert =>
        DayEndSummary?.Discrepancy is { } d && Math.Abs(d) > VarianceAlertThreshold;

    public string CashVarianceAlertMessage =>
        DayEndSummary?.Discrepancy is { } d && Math.Abs(d) > VarianceAlertThreshold
            ? $"Cash variance of {regional.FormatCurrency(Math.Abs(d))} detected ({(d > 0 ? "surplus" : "shortage")}). Please verify."
            : string.Empty;

    [ObservableProperty]
    public partial decimal ExpectedBalance { get; set; }

    public string CurrencySymbol => regional.CurrencySymbol;

    [ObservableProperty]
    public partial ObservableCollection<CashMovement> Movements { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<CashRegister> RegisterHistory { get; set; } = [];

    public bool IsRegisterOpen => OpenRegister is not null;
    public bool IsRegisterClosed => OpenRegister is null;

    // ── Open register form ──

    [ObservableProperty]
    public partial string OpeningBalanceInput { get; set; } = "0";

    // ── Close register form ──

    [ObservableProperty]
    public partial string ClosingBalanceInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CloseNotes { get; set; } = string.Empty;

    // ── Denomination entry (#249) ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count2000 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count500 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count200 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count100 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count50 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count20 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count10 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count5 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenominationTotal))]
    public partial int Count1 { get; set; }

    public decimal DenominationTotal =>
        Count2000 * 2000m + Count500 * 500m + Count200 * 200m + Count100 * 100m +
        Count50 * 50m + Count20 * 20m + Count10 * 10m + Count5 * 5m + Count2 * 2m + Count1 * 1m;

    // ── Movement form ──

    [ObservableProperty]
    public partial string MovementAmount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MovementDirection { get; set; } = "In";

    [ObservableProperty]
    public partial string MovementReason { get; set; } = string.Empty;

    public ObservableCollection<string> Directions { get; } = ["In", "Out"];

    public string FormatCurrency(decimal amount) => regional.FormatCurrency(amount);

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await RefreshStateAsync(ct);
        await LoadHistoryAsync(ct);
    });

    private async Task RefreshStateAsync(CancellationToken ct)
    {
        OpenRegister = await cashRegisterService.GetOpenRegisterAsync(ct);

        if (OpenRegister is not null)
        {
            ExpectedBalance = await cashRegisterService.CalculateExpectedBalanceAsync(OpenRegister.Id, ct);
            Movements = new ObservableCollection<CashMovement>(OpenRegister.Movements.OrderByDescending(m => m.Timestamp));
        }
        else
        {
            ExpectedBalance = 0;
            Movements = [];
        }
    }

    private async Task LoadHistoryAsync(CancellationToken ct)
    {
        var history = await cashRegisterService.GetRegisterHistoryAsync(30, ct);
        RegisterHistory = new ObservableCollection<CashRegister>(history);
    }

    [RelayCommand]
    private Task OpenRegisterAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(decimal.TryParse(OpeningBalanceInput, out var bal) && bal >= 0,
                  "Enter a valid opening balance (0 or more).")))
            return;

        try
        {
            var balance = decimal.Parse(OpeningBalanceInput);
            await cashRegisterService.OpenRegisterAsync(balance, appState.CurrentUserType.ToString(), ct);
            SuccessMessage = $"Register opened with {regional.FormatCurrency(balance)}.";
            OpeningBalanceInput = "0";
            await RefreshStateAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    });

    [RelayCommand]
    private Task CloseRegisterAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (OpenRegister is null)
        {
            ErrorMessage = "No register is currently open.";
            return;
        }

        if (!Validate(v => v
            .Rule(decimal.TryParse(ClosingBalanceInput, out var bal) && bal >= 0,
                  "Enter the actual cash counted.")))
            return;

        try
        {
            var closing = decimal.Parse(ClosingBalanceInput);
            var notes = string.IsNullOrWhiteSpace(CloseNotes) ? null : CloseNotes.Trim();
            await cashRegisterService.CloseRegisterAsync(OpenRegister.Id, closing, notes, appState.CurrentUserType.ToString(), ct);

            DayEndSummary = await cashRegisterService.GetDayEndSummaryAsync(OpenRegister.Id, ct);
            SuccessMessage = "Register closed. Day-end summary available below.";

            // Auto backup on day close (#322)
            _ = Task.Run(async () =>
            {
                try
                {
                    var folder = backupService.GetDefaultBackupFolder();
                    await backupService.BackupAsync(folder, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Auto backup on day close failed");
                }
            }, CancellationToken.None);

            ClosingBalanceInput = string.Empty;
            CloseNotes = string.Empty;
            await RefreshStateAsync(ct);
            await LoadHistoryAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    });

    [RelayCommand]
    private Task RecordMovementAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (OpenRegister is null)
        {
            ErrorMessage = "Open a register before recording movements.";
            return;
        }

        if (!Validate(v => v
            .Rule(decimal.TryParse(MovementAmount, out var amt) && amt > 0, "Enter a valid amount.")
            .Rule(!string.IsNullOrWhiteSpace(MovementReason), "Reason is required.")))
            return;

        try
        {
            var amount = decimal.Parse(MovementAmount);
            await cashRegisterService.RecordMovementAsync(
                OpenRegister.Id, amount, MovementDirection, MovementReason.Trim(),
                appState.CurrentUserType.ToString(), ct);

            SuccessMessage = $"Cash {MovementDirection.ToLowerInvariant()} of {regional.FormatCurrency(amount)} recorded.";
            MovementAmount = string.Empty;
            MovementReason = string.Empty;
            MovementDirection = "In";
            await RefreshStateAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    });

    [RelayCommand]
    private Task ViewDayEndSummaryAsync(CashRegister? register) => RunAsync(async ct =>
    {
        if (register is null) return;
        ClearMessages();
        DayEndSummary = await cashRegisterService.GetDayEndSummaryAsync(register.Id, ct);
    });

    [RelayCommand]
    private void DismissSummary()
    {
        DayEndSummary = null;
    }

    // ── Denomination commands (#249) ──

    [RelayCommand]
    private void ApplyDenomination()
    {
        ClosingBalanceInput = DenominationTotal.ToString("0");
    }

    [RelayCommand]
    private void ResetDenomination()
    {
        Count2000 = 0; Count500 = 0; Count200 = 0; Count100 = 0; Count50 = 0;
        Count20 = 0; Count10 = 0; Count5 = 0; Count2 = 0; Count1 = 0;
    }

    // ── Export commands (#251) ──

    [RelayCommand]
    private void ExportDaySummaryCsv()
    {
        if (DayEndSummary is null) return;
        if (CsvExporter.Export(new[] { DayEndSummary }, "DayEndSummary.csv"))
            SuccessMessage = "Day-end summary exported.";
    }

    [RelayCommand]
    private void ExportRegisterHistoryCsv()
    {
        if (RegisterHistory.Count == 0) return;
        var rows = RegisterHistory.Select(r => new
        {
            r.Id, r.OpenedAt, r.ClosedAt, r.OpeningBalance,
            r.ClosingBalance, r.ExpectedBalance, r.Discrepancy,
            r.OpenedByRole, r.ClosedByRole, r.CloseNotes
        });
        if (CsvExporter.Export(rows, "RegisterHistory.csv"))
            SuccessMessage = "Register history exported.";
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}

