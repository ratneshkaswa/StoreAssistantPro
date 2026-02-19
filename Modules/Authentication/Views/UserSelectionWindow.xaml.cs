using System.Windows;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class UserSelectionWindow : Window
{
    public UserSelectionWindow(UserSelectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
    }
}
