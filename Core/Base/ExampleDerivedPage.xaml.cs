namespace StoreAssistantPro.Core.Base;

/// <summary>
/// Example page demonstrating the <see cref="BasePage"/> layout pattern.
/// Copy this file as a starting point for any new content page.
/// <para>
/// <b>What BasePage provides automatically:</b>
/// <list type="bullet">
///   <item>Standard 20px padding around all content.</item>
///   <item>Page title with optional header toolbar (<c>HeaderContent</c>).</item>
///   <item>Error-message bar bound to <c>BaseViewModel.ErrorMessage</c>.</item>
///   <item>Loading overlay bound to <c>BaseViewModel.IsLoading</c>.</item>
///   <item>Responsive <c>*</c>-sized content row that auto-resizes with the
///         parent window and prevents clipping via the outer
///         <c>ResponsiveContentControl</c> / <c>ViewportConstrainedPanel</c>.</item>
/// </list>
/// </para>
/// </summary>
public partial class ExampleDerivedPage : BasePage
{
    public ExampleDerivedPage()
    {
        InitializeComponent();
    }
}
