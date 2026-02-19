using System.Windows;
using StoreAssistantPro.ViewModels;

namespace StoreAssistantPro.Views;

public partial class UserSelectionWindow : Window
{
    public UserSelectionWindow(UserSelectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
    }
}
