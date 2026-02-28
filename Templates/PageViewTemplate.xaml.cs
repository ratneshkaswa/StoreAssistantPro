// ═══════════════════════════════════════════════════════════════════════
//  PAGE VIEW CODE-BEHIND TEMPLATE
// ═══════════════════════════════════════════════════════════════════════
//
//  1. Replace MODULENAME and VIEWNAME.
//  2. Register the ViewModel → View DataTemplate in App.xaml:
//
//       <DataTemplate DataType="{x:Type vm:VIEWMODELNAME}">
//           <views:VIEWNAME/>
//       </DataTemplate>
//
//  3. Add navigation command to MainViewModel to set CurrentView.
//
//  Rules:
//    • Page views are UserControls, not Windows.
//    • No business logic in code-behind — delegate to ViewModel.
//    • OnLoaded triggers async data loading via ViewModel command.
// ═══════════════════════════════════════════════════════════════════════

using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Modules.MODULENAME.Views;

public partial class VIEWNAME : UserControl
{
    public VIEWNAME()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Replace VIEWMODELNAME with your actual ViewModel type:
        // if (DataContext is VIEWMODELNAME vm)
        //     await vm.LoadCommand.ExecuteAsync(null);
    }
}
