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

    public SetupWindow(SetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this, useMicaAlt: true);

        // Enforce numeric-only input + paste protection on all PIN PasswordBoxes
        PasswordBox[] pinBoxes = [AdminPinBox, AdminPinConfirmBox, ManagerPinBox, ManagerPinConfirmBox,
                                   UserPinBox, UserPinConfirmBox, MasterPinBox, MasterPinConfirmBox];
        foreach (var box in pinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

        // Pincode is also numeric-only
        PincodeBox.PreviewTextInput += OnPreviewNumericOnly;
        DataObject.AddPastingHandler(PincodeBox, OnPastingNumericOnly);

        // Phone allows digits, +, -, spaces
        PhoneBox.PreviewTextInput += OnPreviewPhoneOnly;
        DataObject.AddPastingHandler(PhoneBox, OnPastingPhoneOnly);

        // #8/#9: Force-uppercase pasted text for GSTIN and PAN
        DataObject.AddPastingHandler(GstinBox, OnPastingUpperCase);
        DataObject.AddPastingHandler(PanBox, OnPastingUpperCase);

        PreviewKeyDown += OnPreviewKeyDown;

        // Focus firm name on load
        Loaded += (_, _) => FirmNameBox.Focus();
        Closed += (_, _) => vm.Dispose();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SetupViewModel vm) return;

        // #4: Escape key closes window (triggers OnWindowClosing confirmation)
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        // #5: Ctrl+Enter saves from anywhere
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (!vm.IsBusy && vm.SaveCommand.CanExecute(null))
                vm.SaveCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter) return;

        // IsBusy guard
        if (vm.IsBusy) { e.Handled = true; return; }

        // Don't trigger save when focus is inside editable fields
        if (Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox)
            return;

        if (vm.SaveCommand.CanExecute(null))
            vm.SaveCommand.Execute(null);

        e.Handled = true;
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
                    tb.Text = tb.Text.Insert(caret, upper);
                    tb.CaretIndex = caret + upper.Length;
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