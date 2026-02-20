using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base layout template for all application content pages hosted in
/// <see cref="Controls.ResponsiveContentControl"/>.
/// Provides standard padding, a page title, an optional header region
/// (for toolbar buttons alongside the title), an error-message bar
/// bound to <see cref="BaseViewModel.ErrorMessage"/>, a loading overlay
/// bound to <see cref="BaseViewModel.IsLoading"/>, and a
/// <c>ContentPresenter</c> that fills remaining space with a responsive
/// <c>*</c>-sized Grid row.
/// <para>
/// Combined with <see cref="Controls.ResponsiveContentControl"/> and
/// <see cref="Controls.ViewportConstrainedPanel"/>, this guarantees
/// that star-sized rows resize with the parent window, controls are
/// never clipped, and scrollbars appear only when the content's desired
/// size exceeds the viewport.
/// </para>
/// <para>
/// <b>Architecture rule:</b> Every new content page should use
/// <see cref="BasePage"/> as its root element so that layout chrome
/// (padding, title, error display, loading indicator) is consistent
/// across the application.
/// </para>
/// <para><b>Usage (XAML):</b></para>
/// <code>
/// &lt;core:BasePage x:Class="StoreAssistantPro.Modules.Foo.Views.FooView"
///                xmlns:core="clr-namespace:StoreAssistantPro.Core"
///                PageTitle="Foo"&gt;
///     &lt;Grid&gt;
///         &lt;!-- page-specific content --&gt;
///     &lt;/Grid&gt;
/// &lt;/core:BasePage&gt;
/// </code>
/// </summary>
public class BasePage : ContentControl
{
    /// <summary>
    /// The page title displayed at the top of the layout.
    /// </summary>
    public static readonly DependencyProperty PageTitleProperty =
        DependencyProperty.Register(
            nameof(PageTitle),
            typeof(string),
            typeof(BasePage),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Optional content displayed in the title bar alongside the title
    /// (e.g. toolbar buttons, a search box, a refresh button).
    /// </summary>
    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(BasePage),
            new PropertyMetadata(null));

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    public object HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    static BasePage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BasePage),
            new FrameworkPropertyMetadata(typeof(BasePage)));

        FocusableProperty.OverrideMetadata(
            typeof(BasePage),
            new FrameworkPropertyMetadata(false));
    }
}
