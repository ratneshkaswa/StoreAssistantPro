using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StoreAssistantPro.Tests.Helpers;

internal static class WpfInteractionHelper
{
    public static T FindRequiredName<T>(FrameworkElement root, string name) where T : FrameworkElement
    {
        root.ApplyTemplate();
        root.UpdateLayout();

        var found = root.FindName(name) as T ?? FindVisualChildByName<T>(root, name);
        return found ?? throw new InvalidOperationException($"Could not find element '{name}'.");
    }

    public static T FindRequiredVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        var child = FindVisualChild<T>(root);
        return child ?? throw new InvalidOperationException($"Could not find visual child of type '{typeof(T).Name}'.");
    }

    public static void Click(ButtonBase button)
    {
        if (button.Command?.CanExecute(button.CommandParameter) == true)
            button.Command.Execute(button.CommandParameter);

        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
    }

    private static T? FindVisualChildByName<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild && string.Equals(typedChild.Name, name, StringComparison.Ordinal))
                return typedChild;

            var nested = FindVisualChildByName<T>(child, name);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
                return typedChild;

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
