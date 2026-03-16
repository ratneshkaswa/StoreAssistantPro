using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Inward.ViewModels;

namespace StoreAssistantPro.Modules.Inward.Views;

public partial class InwardEntryView : UserControl
{
    public InwardEntryView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InwardEntryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
