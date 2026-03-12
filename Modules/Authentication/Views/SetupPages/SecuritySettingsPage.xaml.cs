using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class SecuritySettingsPage : UserControl
{
    private bool _handlersAttached;
    private bool _isSyncingFromViewModel;
    private SetupViewModel? _boundViewModel;

    public SecuritySettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_handlersAttached) return;
        _handlersAttached = true;

        PasswordBox[] pinBoxes = [AdminPinBox, AdminPinConfirmBox, UserPinBox, UserPinConfirmBox, MasterPinBox, MasterPinConfirmBox];
        foreach (var box in pinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

        TextBox[] pinTextBoxes =
        [
            AdminPinTextBox, AdminPinConfirmTextBox,
            UserPinTextBox, UserPinConfirmTextBox,
            MasterPinTextBox, MasterPinConfirmTextBox
        ];
        foreach (var box in pinTextBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

        if (DataContext is SetupViewModel vm)
            AttachToViewModel(vm);
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromViewModel) return;
        if (DataContext is not SetupViewModel vm) return;

        if (sender == AdminPinBox)
            vm.AdminPin = AdminPinBox.Password;
        else if (sender == AdminPinConfirmBox)
            vm.AdminPinConfirm = AdminPinConfirmBox.Password;
        else if (sender == UserPinBox)
            vm.UserPin = UserPinBox.Password;
        else if (sender == UserPinConfirmBox)
            vm.UserPinConfirm = UserPinConfirmBox.Password;
        else if (sender == MasterPinBox)
            vm.MasterPin = MasterPinBox.Password;
        else if (sender == MasterPinConfirmBox)
            vm.MasterPinConfirm = MasterPinConfirmBox.Password;

        if (sender is PasswordBox pb
            && pb.IsKeyboardFocused
            && pb.Password.Length == pb.MaxLength)
            AdvancePinFocus(pb);
    }

    private void OnPinTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb
            && tb.IsKeyboardFocused
            && tb.Text.Length == tb.MaxLength)
            AdvancePinFocus(tb);
    }

    private void AdvancePinFocus(Control current)
    {
        Control? next = current == AdminPinBox || current == AdminPinTextBox
            ? GetRolePinInput(AdminPinConfirmBox, AdminPinConfirmTextBox)
            : current == AdminPinConfirmBox || current == AdminPinConfirmTextBox
                ? GetRolePinInput(UserPinBox, UserPinTextBox)
            : current == UserPinBox || current == UserPinTextBox
                ? GetRolePinInput(UserPinConfirmBox, UserPinConfirmTextBox)
            : current == UserPinConfirmBox || current == UserPinConfirmTextBox
                ? GetMasterPinInput(MasterPinBox, MasterPinTextBox)
            : current == MasterPinBox || current == MasterPinTextBox
                ? GetMasterPinInput(MasterPinConfirmBox, MasterPinConfirmTextBox)
            : null;

        FocusInput(next);
    }

    /// <summary>
    /// Clears all PasswordBox controls. Called when the ViewModel clears
    /// sensitive PIN strings so the UI stays in sync (PasswordBox does
    /// not support data binding on Password).
    /// </summary>
    public void ClearAllPinBoxes()
    {
        MasterPinBox.Clear();
        MasterPinConfirmBox.Clear();
        AdminPinBox.Clear();
        AdminPinConfirmBox.Clear();
        UserPinBox.Clear();
        UserPinConfirmBox.Clear();
        MasterPinTextBox.Clear();
        MasterPinConfirmTextBox.Clear();
        AdminPinTextBox.Clear();
        AdminPinConfirmTextBox.Clear();
        UserPinTextBox.Clear();
        UserPinConfirmTextBox.Clear();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SetupViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        _boundViewModel = null;

        if (e.NewValue is SetupViewModel newVm)
            AttachToViewModel(newVm);
    }

    private void AttachToViewModel(SetupViewModel vm)
    {
        _boundViewModel = vm;
        vm.PropertyChanged -= OnViewModelPropertyChanged;
        vm.PropertyChanged += OnViewModelPropertyChanged;
        SyncPasswordBoxesFromViewModel(vm);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SetupViewModel vm) return;
        if (e.PropertyName is nameof(SetupViewModel.AdminPin)
            or nameof(SetupViewModel.AdminPinConfirm)
            or nameof(SetupViewModel.UserPin)
            or nameof(SetupViewModel.UserPinConfirm)
            or nameof(SetupViewModel.MasterPin)
            or nameof(SetupViewModel.MasterPinConfirm)
            or nameof(SetupViewModel.ShowRolePins)
            or nameof(SetupViewModel.ShowMasterPins))
        {
            SyncPasswordBoxesFromViewModel(vm);
        }
    }

    private void SyncPasswordBoxesFromViewModel(SetupViewModel vm)
    {
        _isSyncingFromViewModel = true;
        try
        {
            SetPasswordIfDifferent(AdminPinBox, vm.AdminPin);
            SetPasswordIfDifferent(AdminPinConfirmBox, vm.AdminPinConfirm);
            SetPasswordIfDifferent(UserPinBox, vm.UserPin);
            SetPasswordIfDifferent(UserPinConfirmBox, vm.UserPinConfirm);
            SetPasswordIfDifferent(MasterPinBox, vm.MasterPin);
            SetPasswordIfDifferent(MasterPinConfirmBox, vm.MasterPinConfirm);
        }
        finally
        {
            _isSyncingFromViewModel = false;
        }
    }

    private static void SetPasswordIfDifferent(PasswordBox box, string value)
    {
        value ??= string.Empty;
        if (!string.Equals(box.Password, value, StringComparison.Ordinal))
            box.Password = value;
    }

    private Control GetRolePinInput(PasswordBox hiddenBox, TextBox visibleBox) =>
        _boundViewModel?.ShowRolePins == true ? visibleBox : hiddenBox;

    private Control GetMasterPinInput(PasswordBox hiddenBox, TextBox visibleBox) =>
        _boundViewModel?.ShowMasterPins == true ? visibleBox : hiddenBox;

    private static void FocusInput(Control? control)
    {
        if (control is null)
            return;

        control.Focus();
        if (control is TextBox textBox)
            textBox.SelectAll();
    }

    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

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

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
