using System.Windows;
using System.Windows.Input;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all modal dialog windows.  Provides the complete
/// enterprise dialog standard out of the box:
/// <list type="bullet">
///   <item><b>Fixed size</b> — set via <see cref="DialogWidth"/> / <see cref="DialogHeight"/>.</item>
///   <item><b>No resize</b> — <c>ResizeMode.NoResize</c>.</item>
///   <item><b>Centered over owner</b> — via <see cref="IWindowSizingService"/>.</item>
///   <item><b>Modal</b> — always shown with <c>ShowDialog()</c>.</item>
///   <item><b>Enter = confirm</b> — bind <see cref="ConfirmCommand"/> in XAML.</item>
///   <item><b>ESC = cancel</b> — auto-wired to close with <c>DialogResult = false</c>.</item>
///   <item><b>Auto-focus</b> — first input receives focus on load (global style).</item>
/// </list>
///
/// <para>
/// <b>Architecture rule:</b> Every dialog window in every module must
/// inherit from <see cref="BaseDialogWindow"/>.  Sizing attributes
/// (<c>Height</c>, <c>Width</c>, <c>ResizeMode</c>,
/// <c>WindowStartupLocation</c>) must not be set in XAML — the
/// base class handles them.
/// </para>
///
/// <para><b>XAML usage:</b></para>
/// <code>
/// &lt;core:BaseDialogWindow x:Class="…"
///         ConfirmCommand="{Binding SaveCommand}"
///         Title="Edit Item"&gt;
///     &lt;!-- dialog content --&gt;
/// &lt;/core:BaseDialogWindow&gt;
/// </code>
///
/// <para><b>Code-behind:</b></para>
/// <code>
/// public partial class FirmManagementWindow : BaseDialogWindow
/// {
///     protected override double DialogWidth  =&gt; 450;
///     protected override double DialogHeight =&gt; 350;
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
    // ── Size ─────────────────────────────────────────────────────────

    /// <summary>Fixed width for this dialog.</summary>
    protected abstract double DialogWidth { get; }

    /// <summary>Fixed height for this dialog.</summary>
    protected abstract double DialogHeight { get; }

    // ── Enter = confirm ──────────────────────────────────────────────

    /// <summary>
    /// Primary-action command executed when Enter is pressed on any input
    /// control inside the dialog.  Bind to the same <c>ICommand</c> used
    /// on the primary action button (Save, Submit, etc.) so that
    /// <c>CanExecute</c> guards both the button and the Enter key.
    /// <para>
    /// Internally wires <see cref="KeyboardNav.DefaultCommandProperty"/>
    /// on this window — no manual <c>h:KeyboardNav.DefaultCommand</c>
    /// attribute is needed.
    /// </para>
    /// </summary>
    public static readonly DependencyProperty ConfirmCommandProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommand),
            typeof(ICommand),
            typeof(BaseDialogWindow),
            new PropertyMetadata(null, OnConfirmCommandChanged));

    /// <summary>
    /// Optional parameter passed to <see cref="ConfirmCommand"/>.
    /// </summary>
    public static readonly DependencyProperty ConfirmCommandParameterProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommandParameter),
            typeof(object),
            typeof(BaseDialogWindow),
            new PropertyMetadata(null, OnConfirmCommandParameterChanged));

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public object? ConfirmCommandParameter
    {
        get => GetValue(ConfirmCommandParameterProperty);
        set => SetValue(ConfirmCommandParameterProperty, value);
    }

    private static void OnConfirmCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseDialogWindow window)
            KeyboardNav.SetDefaultCommand(window, (ICommand?)e.NewValue);
    }

    private static void OnConfirmCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseDialogWindow window)
            KeyboardNav.SetDefaultCommandParameter(window, e.NewValue);
    }

    // ── ESC = cancel ─────────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c> (default), pressing ESC closes the dialog with
    /// <c>DialogResult = false</c>.  Override and return <c>false</c> to
    /// disable, letting ESC fall through to <c>IsCancel</c> buttons or
    /// the focus-clear fallback instead.
    /// </summary>
    protected virtual bool CloseOnEscape => true;

    // ── Constructor ──────────────────────────────────────────────────

    public BaseDialogWindow(IWindowSizingService sizingService)
    {
        sizingService.ConfigureDialogWindow(this, DialogWidth, DialogHeight);

        if (CloseOnEscape)
            KeyboardNav.SetEscapeCommand(this, new CloseDialogCommand(this));
    }

    // ── Close command ────────────────────────────────────────────────

    /// <summary>
    /// Lightweight <see cref="ICommand"/> that closes its owning dialog
    /// with <c>DialogResult = false</c>.
    /// </summary>
    private sealed class CloseDialogCommand(Window dialog) : ICommand
    {
        public bool CanExecute(object? parameter) => dialog.IsLoaded;

        public void Execute(object? parameter)
        {
            // Setting DialogResult on a modal window also calls Close().
            // Guard against non-modal usage (ConfigureDialogWindow always
            // uses ShowDialog, but be defensive).
            try
            {
                dialog.DialogResult ??= false;
            }
            catch (InvalidOperationException)
            {
                dialog.Close();
            }
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
