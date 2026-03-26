using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;
using StoreAssistantPro.Modules.Settings.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("UserPreferences")]
public sealed class ViewBitmapSmokeTests : IDisposable
{
    public ViewBitmapSmokeTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public void WorkspaceView_Should_Render_NonBlank_Bitmap()
    {
        var differentPixels = WpfTestApplication.Run(() =>
        {
            var dashboardService = Substitute.For<IDashboardService>();
            dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>())
                .Returns(DashboardSummary.Empty);

            var regional = Substitute.For<IRegionalSettingsService>();
            regional.Now.Returns(new DateTime(2026, 3, 25, 9, 30, 0));
            regional.CurrencySymbol.Returns("₹");
            regional.FormatDate(Arg.Any<DateTime>())
                .Returns(call => ((DateTime)call[0]).ToString("dd-MMM-yyyy"));

            var viewModel = new WorkspaceViewModel(dashboardService, regional);
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            var view = new WorkspaceView
            {
                DataContext = viewModel,
                Width = 1280,
                Height = 900
            };

            return CountDifferentPixels(view, 1280, 900);
        });

        Assert.True(differentPixels > 5000, $"WorkspaceView rendered only {differentPixels} non-background pixels.");
    }

    [Fact]
    public void SystemSettingsView_Should_Render_NonBlank_Bitmap()
    {
        var differentPixels = WpfTestApplication.Run(() =>
        {
            var settingsService = Substitute.For<ISystemSettingsService>();
            settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings
            {
                BackupLocation = "C:\\Backups",
                BackupTime = "09:30",
                DefaultPrinter = "Counter Printer",
                PrinterWidth = 58,
                DefaultPageSize = "Thermal",
                AutoLogoutMinutes = 15
            });

            var viewModel = new SystemSettingsViewModel(
                settingsService,
                Substitute.For<ILoginService>(),
                Substitute.For<IDialogService>(),
                Substitute.For<IUiDensityService>());
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            var view = new SystemSettingsView
            {
                DataContext = viewModel,
                Width = 1320,
                Height = 940
            };

            return CountDifferentPixels(view, 1320, 940);
        });

        Assert.True(differentPixels > 4000, $"SystemSettingsView rendered only {differentPixels} non-background pixels.");
    }

    private static int CountDifferentPixels(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(height));
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);

        var stride = pixelWidth * 4;
        var pixels = new byte[stride * pixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        var baselineBlue = pixels[0];
        var baselineGreen = pixels[1];
        var baselineRed = pixels[2];
        var differentPixels = 0;

        for (var index = 0; index < pixels.Length; index += 4)
        {
            var blue = pixels[index];
            var green = pixels[index + 1];
            var red = pixels[index + 2];
            var alpha = pixels[index + 3];

            if (alpha == 0)
                continue;

            if (Math.Abs(red - baselineRed) > 8 ||
                Math.Abs(green - baselineGreen) > 8 ||
                Math.Abs(blue - baselineBlue) > 8)
            {
                differentPixels++;
            }
        }

        return differentPixels;
    }
}
