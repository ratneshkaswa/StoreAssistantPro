using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StoreAssistantPro.Core;

/// <summary>
/// Reusable PIN pad logic for any WPF MVVM dialog that needs numeric PIN entry.
/// <para>
/// Provides digit entry, backspace, clear, max-length enforcement, and a
/// <see cref="PinCompleted"/> callback that fires when the PIN reaches
/// <see cref="MaxLength"/> digits.
/// </para>
/// <para><b>Usage (composition):</b></para>
/// <code>
/// public PinPadViewModel PinPad { get; } = new(maxLength: 4);
/// 
/// // In constructor:
/// PinPad.PinCompleted += () => LoginCommand.Execute(null);
/// </code>
/// <para><b>XAML binding:</b></para>
/// <code>
/// Command="{Binding PinPad.AddDigitCommand}" CommandParameter="5"
/// Fill="{Binding PinPad.PinLength, Converter=...}"
/// </code>
/// </summary>
public partial class PinPadViewModel : ObservableObject
{
    /// <summary>Maximum number of PIN digits allowed.</summary>
    public int MaxLength { get; }

    /// <summary>Fired when PIN reaches <see cref="MaxLength"/> digits.</summary>
    public event Action? PinCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinLength))]
    public partial string Pin { get; set; }

    public int PinLength => Pin.Length;

    private bool _isLocked;

    public PinPadViewModel(int maxLength = 4)
    {
        MaxLength = maxLength;
        Pin = string.Empty;
    }

    /// <summary>Prevents input while an async operation is in progress.</summary>
    public void Lock() => _isLocked = true;

    /// <summary>Re-enables input after an async operation completes.</summary>
    public void Unlock() => _isLocked = false;

    /// <summary>Clears the PIN (does not fire <see cref="PinCompleted"/>).</summary>
    public void Reset()
    {
        Pin = string.Empty;
    }

    [RelayCommand]
    private void AddDigit(string digit)
    {
        if (_isLocked || Pin.Length >= MaxLength || digit.Length != 1 || !char.IsDigit(digit[0]))
            return;

        Pin += digit;

        if (Pin.Length == MaxLength)
            PinCompleted?.Invoke();
    }

    [RelayCommand]
    private void Backspace()
    {
        if (_isLocked || Pin.Length == 0)
            return;

        Pin = Pin[..^1];
    }

    [RelayCommand]
    private void Clear()
    {
        Pin = string.Empty;
    }
}
