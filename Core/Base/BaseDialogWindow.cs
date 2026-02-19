using System.Windows;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all modal dialog windows. Automatically applies
/// enterprise sizing rules via <see cref="IWindowSizingService"/>:
/// fixed size, no resize, centered over the main window.
/// <para>
/// <b>Architecture rule:</b> Every dialog window in every module must
/// inherit from <see cref="BaseDialogWindow"/>. Sizing attributes
/// (<c>Height</c>, <c>Width</c>, <c>ResizeMode</c>,
/// <c>WindowStartupLocation</c>) must not be set in XAML — the
/// base class handles them.
/// </para>
/// <para>Usage:</para>
/// <code>
/// public partial class FirmManagementWindow : BaseDialogWindow
/// {
///     protected override double DialogWidth => 450;
///     protected override double DialogHeight => 350;
///
///     public FirmManagementWindow(IWindowSizingService sizing, FirmManagementViewModel vm)
///         : base(sizing)
///     {
///         InitializeComponent();
///         DataContext = vm;
///     }
/// }
/// </code>
/// </summary>
public abstract class BaseDialogWindow : Window
{
    /// <summary>Fixed width for this dialog.</summary>
    protected abstract double DialogWidth { get; }

    /// <summary>Fixed height for this dialog.</summary>
    protected abstract double DialogHeight { get; }

    public BaseDialogWindow(IWindowSizingService sizingService)
    {
        sizingService.ConfigureDialogWindow(this, DialogWidth, DialogHeight);
    }
}
