using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;
    private Button? _activeSidebarButton;

    public SetupWindow(SetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        // Enforce numeric-only input + paste protection on all PIN PasswordBoxes
        PasswordBox[] pinBoxes = [AdminPinBox, AdminPinConfirmBox, ManagerPinBox, ManagerPinConfirmBox,
                                   UserPinBox, UserPinConfirmBox, MasterPinBox, MasterPinConfirmBox];
        foreach (var box in pinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

        // Phone allows digits, +, -, spaces
        PhoneBox.PreviewTextInput += OnPreviewPhoneOnly;
        DataObject.AddPastingHandler(PhoneBox, OnPastingPhoneOnly);

        // #8/#9: Force-uppercase pasted text for GSTIN and PAN
        DataObject.AddPastingHandler(GstinBox, OnPastingUpperCase);
        DataObject.AddPastingHandler(PanBox, OnPastingUpperCase);

        vm.PropertyChanged += OnViewModelPropertyChanged;

        // Set initial active sidebar button (AutoFocus handles keyboard focus globally)
        Loaded += (_, _) =>
        {
            SetActiveSidebarButton(SidebarFirmButton);
        };
        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SetupViewModel vm || e.PropertyName != nameof(SetupViewModel.ErrorMessage))
            return;

        if (string.IsNullOrWhiteSpace(vm.ErrorMessage))
            return;

        Dispatcher.BeginInvoke(() => FocusFirstInvalidField(vm.ErrorMessage));
    }

    private void FocusFirstInvalidField(string error)
    {
        if (error.Contains("Firm name", StringComparison.OrdinalIgnoreCase)) { FirmNameBox.Focus(); return; }
        if (error.Contains("Admin PIN", StringComparison.OrdinalIgnoreCase)) { AdminPinBox.Focus(); return; }
        if (error.Contains("Manager PIN", StringComparison.OrdinalIgnoreCase)) { ManagerPinBox.Focus(); return; }
        if (error.Contains("User PIN", StringComparison.OrdinalIgnoreCase)) { UserPinBox.Focus(); return; }
        if (error.Contains("Master PIN", StringComparison.OrdinalIgnoreCase)) { MasterPinBox.Focus(); return; }
        if (error.Contains("GSTIN", StringComparison.OrdinalIgnoreCase)) { GstinBox.Focus(); return; }
        if (error.Contains("PAN", StringComparison.OrdinalIgnoreCase)) { PanBox.Focus(); return; }
        if (error.Contains("Pincode", StringComparison.OrdinalIgnoreCase)) { PincodeBox.Focus(); return; }
        if (error.Contains("Email", StringComparison.OrdinalIgnoreCase)) { EmailBox.Focus(); return; }
        if (error.Contains("Phone", StringComparison.OrdinalIgnoreCase)) { PhoneBox.Focus(); }
    }

    private void OnSidebarSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string targetName)
            return;

        SetActiveSidebarButton(button);

        if (FindName(targetName) is FrameworkElement target)
            target.BringIntoView();
    }

    private void SetActiveSidebarButton(Button button)
    {
        if (_activeSidebarButton is not null)
        {
            _activeSidebarButton.ClearValue(BackgroundProperty);
            _activeSidebarButton.ClearValue(BorderBrushProperty);
        }

        _activeSidebarButton = button;
        if (FindResource("FluentSubtleHover") is System.Windows.Media.Brush activeBackground)
            button.Background = activeBackground;
        if (FindResource("FluentAccentDefault") is System.Windows.Media.Brush activeBorder)
            button.BorderBrush = activeBorder;
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SetupViewModel vm) return;

        if (sender == AdminPinBox)
            vm.AdminPin = AdminPinBox.Password;
        else if (sender == AdminPinConfirmBox)
            vm.AdminPinConfirm = AdminPinConfirmBox.Password;
        else if (sender == ManagerPinBox)
            vm.ManagerPin = ManagerPinBox.Password;
        else if (sender == ManagerPinConfirmBox)
            vm.ManagerPinConfirm = ManagerPinConfirmBox.Password;
        else if (sender == UserPinBox)
            vm.UserPin = UserPinBox.Password;
        else if (sender == UserPinConfirmBox)
            vm.UserPinConfirm = UserPinConfirmBox.Password;
        else if (sender == MasterPinBox)
            vm.MasterPin = MasterPinBox.Password;
        else if (sender == MasterPinConfirmBox)
            vm.MasterPinConfirm = MasterPinConfirmBox.Password;

        // Auto-advance focus when PIN reaches max length
        if (sender is PasswordBox pb && pb.Password.Length == pb.MaxLength)
            AdvancePinFocus(pb);
    }

    private void AdvancePinFocus(PasswordBox current)
    {
        PasswordBox? next = current == AdminPinBox ? AdminPinConfirmBox
            : current == AdminPinConfirmBox ? ManagerPinBox
            : current == ManagerPinBox ? ManagerPinConfirmBox
            : current == ManagerPinConfirmBox ? UserPinBox
            : current == UserPinBox ? UserPinConfirmBox
            : current == MasterPinBox ? MasterPinConfirmBox
            : null;

        next?.Focus();
    }

    /// <summary>Only allow digit characters in PIN fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    /// <summary>Reject non-numeric clipboard paste in PIN fields.</summary>
    private static void OnPastingNumericOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!DigitsOnlyRegex().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    /// <summary>
    /// #8/#9: Force-uppercase pasted text for GSTIN/PAN.
    /// <c>CharacterCasing="Upper"</c> only affects typed input; pasted text stays lowercase.
    /// </summary>
    private static void OnPastingUpperCase(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            var upper = text.ToUpperInvariant();
            if (upper != text)
            {
                e.CancelCommand();
                if (sender is TextBox tb)
                {
                    var caret = tb.CaretIndex;
                    var available = tb.MaxLength > 0 ? tb.MaxLength - tb.Text.Length : int.MaxValue;
                    var insert = upper[..Math.Min(upper.Length, available)];
                    if (insert.Length > 0)
                    {
                        tb.Text = tb.Text.Insert(caret, insert);
                        tb.CaretIndex = caret + insert.Length;
                    }
                }
            }
        }
    }

    /// <summary>Confirm close if setup is in progress.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null || _vm.IsSetupComplete || DialogResult == true) return;

        // Prevent close during processing
        if (_vm.IsBusy) { e.Cancel = true; return; }

        var result = MessageBox.Show(
            "Setup is not complete. Are you sure you want to cancel?",
            "Cancel Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
            e.Cancel = true;
        else
            _vm.ClearSensitivePins();
    }

    /// <summary>Allow only digits, +, -, and spaces in Phone field.</summary>
    private static void OnPreviewPhoneOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !PhoneCharRegex().IsMatch(e.Text);
    }

    /// <summary>Reject paste that contains invalid phone characters.</summary>
    private static void OnPastingPhoneOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!SetupViewModel.PhoneInputRegex().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();

    [GeneratedRegex(@"^[\d\s\+\-]$")]
    private static partial Regex PhoneCharRegex();
}