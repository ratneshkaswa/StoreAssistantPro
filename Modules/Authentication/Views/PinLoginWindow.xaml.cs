using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class PinLoginWindow : Window
{
    public PinLoginWindow(PinLoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;

        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is PinLoginViewModel vm)
            vm.Pin = PinBox.Password;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PinLoginViewModel.Pin) &&
            DataContext is PinLoginViewModel vm &&
            vm.Pin != PinBox.Password)
        {
            PinBox.Password = vm.Pin;
        }
    }
}
