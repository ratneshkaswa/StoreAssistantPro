// ═══════════════════════════════════════════════════════════════════════
//  DIALOG WINDOW CODE-BEHIND TEMPLATE
// ═══════════════════════════════════════════════════════════════════════
//
//  1. Replace MODULENAME, WINDOWNAME, and VIEWMODELNAME.
//  2. Set DialogWidth / DialogHeight for this dialog's content.
//  3. If the dialog needs data on load, uncomment the OnLoaded section
//     and add Loaded="OnLoaded" to the XAML root element.
//
//  Rules:
//    • Every dialog must inherit BaseDialogWindow.
//    • Sizing is set here — never in XAML.
//    • IWindowSizingService is required (injected via DI).
//    • DataContext is assigned in the constructor.
// ═══════════════════════════════════════════════════════════════════════

using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MODULENAME.ViewModels;

namespace StoreAssistantPro.Modules.MODULENAME.Views;

public partial class WINDOWNAME : BaseDialogWindow
{
    // ── Fixed dialog dimensions ──────────────────────────────────────
    protected override double DialogWidth  => 500;
    protected override double DialogHeight => 400;

    public WINDOWNAME(
        IWindowSizingService sizingService,
        VIEWMODELNAME vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
    }

    // ── Optional: load data when the dialog opens ────────────────────
    //
    // private async void OnLoaded(object sender, RoutedEventArgs e)
    // {
    //     if (DataContext is VIEWMODELNAME vm)
    //         await vm.LoadCommand.ExecuteAsync(null);
    // }
}
