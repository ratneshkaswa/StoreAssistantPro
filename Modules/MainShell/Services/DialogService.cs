using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Billing.Views;

namespace StoreAssistantPro.Modules.MainShell.Services;

public class DialogService(
    IWindowRegistry windowRegistry,
    IServiceProvider serviceProvider) : IDialogService
{
    public bool Confirm(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(
            message, title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public string? PromptPassword(string message, string title = "Authentication Required")
    {
        string? enteredValue = null;

        var window = new Window
        {
            Title = title,
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow
        };

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var passwordBox = new PasswordBox
        {
            MaxLength = 6,
            Margin = new Thickness(0, 0, 0, 14)
        };
        panel.Children.Add(passwordBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 75,
            IsDefault = true,
            Margin = new Thickness(0, 0, 8, 0)
        };
        okButton.Click += (_, _) =>
        {
            enteredValue = passwordBox.Password;
            window.DialogResult = true;
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 75,
            IsCancel = true
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);

        window.Content = panel;
        window.Loaded += (_, _) => passwordBox.Focus();

        return window.ShowDialog() == true ? enteredValue : null;
    }

    public bool? ShowDialog(string dialogKey) =>
        windowRegistry.ShowDialog(dialogKey);

    public bool ShowResumeBillingDialog(BillingSession session, UserType currentUserType)
    {
        var regional = serviceProvider.GetRequiredService<IRegionalSettingsService>();
        var sizing = serviceProvider.GetRequiredService<IWindowSizingService>();

        var vm = new ResumeBillingDialogViewModel(session, currentUserType, regional);
        var dialog = new ResumeBillingDialog(sizing, vm);

        dialog.ShowDialog();
        return vm.UserChoseResume;
    }
}
