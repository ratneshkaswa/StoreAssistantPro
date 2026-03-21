using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shared routed commands for TextBox templates.
/// </summary>
public static class TextBoxCommands
{
    public static RoutedUICommand ClearTextCommand { get; } =
        new("Clear text", nameof(ClearTextCommand), typeof(TextBoxCommands));

    static TextBoxCommands()
    {
        CommandManager.RegisterClassCommandBinding(
            typeof(TextBox),
            new CommandBinding(ClearTextCommand, OnClearTextExecuted, OnCanClearText));
    }

    private static void OnCanClearText(object sender, CanExecuteRoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        e.CanExecute = textBox.IsEnabled
            && !textBox.IsReadOnly
            && !string.IsNullOrEmpty(textBox.Text);
        e.Handled = true;
    }

    private static void OnClearTextExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.Clear();
        textBox.Focus();
        e.Handled = true;
    }
}
