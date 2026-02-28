using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

/// <summary>
/// ViewModel for the Resume Billing dialog shown at startup when a
/// persisted billing session is found.
/// <para>
/// Presents session metadata (start time, user, item count) and exposes
/// Resume / Discard actions that set <see cref="UserChoseResume"/> and
/// close the dialog.
/// </para>
/// </summary>
public partial class ResumeBillingDialogViewModel : BaseViewModel
{
    // ── Session info (read-only display) ───────────────────────────

    public string BillStartTime { get; }
    public string UserDisplay { get; }
    public int ItemCount { get; }
    public string ElapsedTime { get; }

    // ── Result ─────────────────────────────────────────────────────

    [ObservableProperty]
    public partial bool UserChoseResume { get; set; }

    /// <summary>
    /// Callback set by the dialog's code-behind to close the window
    /// with the correct <c>DialogResult</c>.
    /// </summary>
    public Action<bool>? CloseDialog { get; set; }

    public ResumeBillingDialogViewModel(
        BillingSession session,
        UserType currentUserType,
        IRegionalSettingsService regional)
    {
        BillStartTime = regional.FormatDateTime(session.CreatedTime);
        UserDisplay = currentUserType.ToString();
        ElapsedTime = FormatElapsed(regional.Now - session.LastUpdated);

        // Try to extract item count from the serialized data
        ItemCount = TryGetItemCount(session.SerializedBillData);
    }

    // ── Commands ───────────────────────────────────────────────────

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Resume()
    {
        UserChoseResume = true;
        CloseDialog?.Invoke(true);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Discard()
    {
        UserChoseResume = false;
        CloseDialog?.Invoke(false);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static int TryGetItemCount(string serializedBillData)
    {
        try
        {
            var cart = JsonSerializer.Deserialize<SerializedCart>(
                serializedBillData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return cart?.Items.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes} min ago";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m ago";
        return $"{(int)elapsed.TotalDays}d {elapsed.Hours}h ago";
    }
}
