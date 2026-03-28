using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;
using StoreAssistantPro.Modules.Firm.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("UserPreferences")]
public sealed class FirmSetupRenderDiagnosticsTests
{
    [Fact]
    public void FirmSetupView_Should_Not_Expose_Invalid_Border_Backgrounds_After_Delayed_Render()
    {
        var failures = WpfTestApplication.Run(() =>
        {
            var diagnostics = new List<string>();
            DispatcherUnhandledExceptionEventHandler? exceptionHandler = null;
            exceptionHandler = (_, args) =>
            {
                diagnostics.Add($"DispatcherUnhandledException: {args.Exception.GetType().Name}: {args.Exception.Message}");
                args.Handled = true;
            };
            Dispatcher.CurrentDispatcher.UnhandledException += exceptionHandler;

            var firmService = Substitute.For<IFirmService>();
            firmService.GetFirmAsync(Arg.Any<CancellationToken>())
                .Returns(new FirmManagementSnapshot(
                    FirmName: string.Empty,
                    Address: string.Empty,
                    State: string.Empty,
                    Pincode: string.Empty,
                    Phone: string.Empty,
                    Email: string.Empty,
                    GSTNumber: string.Empty,
                    PANNumber: string.Empty,
                    GstRegistrationType: "Regular",
                    CompositionSchemeRate: 1m,
                    StateCode: null,
                    FinancialYearStartMonth: 4,
                    FinancialYearEndMonth: 3,
                    CurrencySymbol: "\u20B9",
                    DateFormat: "dd/MM/yyyy",
                    NumberFormat: "en-IN",
                    DefaultTaxMode: "Exclusive",
                    RoundingMethod: "No Rounding",
                    NegativeStockAllowed: false,
                    NumberToWordsLanguage: "English",
                    InvoicePrefix: "INV",
                    ReceiptFooterText: "Thank you",
                    LogoPath: string.Empty,
                    BankName: string.Empty,
                    BankAccountNumber: string.Empty,
                    BankIFSC: string.Empty,
                    ReceiptHeaderText: string.Empty,
                    InvoiceResetPeriod: "Never",
                    IsInitialSetupPending: true));

            var appState = Substitute.For<IAppStateService>();
            appState.IsInitialSetupPending.Returns(true);

            var viewModel = new FirmViewModel(
                firmService,
                Substitute.For<IEventBus>(),
                appState);

            var view = new FirmManagementView
            {
                DataContext = viewModel
            };

            var host = new Window
            {
                Width = 1280,
                Height = 900,
                ShowActivated = false,
                ShowInTaskbar = false,
                Left = -10000,
                Top = -10000,
                Content = view
            };

            host.Show();
            host.UpdateLayout();
            WpfTestApplication.FlushDispatcher();
            WpfTestApplication.WaitForDispatcher(TimeSpan.FromSeconds(6));
            host.UpdateLayout();
            WpfTestApplication.FlushDispatcher();

            AuditBorders(view, diagnostics);

            host.Close();
            Dispatcher.CurrentDispatcher.UnhandledException -= exceptionHandler;
            return diagnostics;
        });

        Assert.True(
            failures.Count == 0,
            "Invalid border backgrounds detected:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static void AuditBorders(DependencyObject root, List<string> failures)
    {
        foreach (var border in EnumerateTree(root).OfType<Border>())
        {
            try
            {
                _ = border.Background;
            }
            catch (Exception ex)
            {
                failures.Add($"{DescribeElement(border)} => {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private static IEnumerable<DependencyObject> EnumerateTree(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = childCount - 1; i >= 0; i--)
            {
                stack.Push(VisualTreeHelper.GetChild(current, i));
            }
        }
    }

    private static string DescribeElement(Border border)
    {
        var builder = new StringBuilder();
        DependencyObject? current = border;

        while (current is not null)
        {
            if (builder.Length > 0)
                builder.Insert(0, " <- ");

            builder.Insert(0, current switch
            {
                FrameworkElement frameworkElement when !string.IsNullOrWhiteSpace(frameworkElement.Name)
                    => $"{frameworkElement.GetType().Name}#{frameworkElement.Name}",
                _ => current.GetType().Name
            });

            current = VisualTreeHelper.GetParent(current);
        }

        return builder.ToString();
    }
}
