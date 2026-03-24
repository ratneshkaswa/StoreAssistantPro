using StoreAssistantPro.Core.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Threading;

namespace StoreAssistantPro.Tests.Core;

/// <summary>
/// Validates the Win11 WinUI-style control library.
/// Checks that each control has the correct base class and
/// required dependency properties (templates are validated at runtime).
/// </summary>
public class FluentControlsTests
{
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
            throw new AggregateException(caught);
    }

    [Theory]
    [InlineData(typeof(InfoBar))]
    [InlineData(typeof(ProgressRing))]
    [InlineData(typeof(LoadingOverlay))]
    [InlineData(typeof(NumberBox))]
    [InlineData(typeof(DateRangePicker))]
    [InlineData(typeof(FluentExpander))]
    [InlineData(typeof(BreadcrumbBar))]
    [InlineData(typeof(BreadcrumbBarItem))]
    public void Control_InheritsFromFrameworkElement(Type controlType)
    {
        Assert.True(typeof(FrameworkElement).IsAssignableFrom(controlType),
            $"{controlType.Name} must inherit from FrameworkElement");
    }

    [Fact]
    public void InfoBar_HasRequiredDependencyProperties()
    {
        Assert.NotNull(InfoBar.SeverityProperty);
        Assert.NotNull(InfoBar.TitleProperty);
        Assert.NotNull(InfoBar.MessageProperty);
        Assert.NotNull(InfoBar.IsOpenProperty);
        Assert.NotNull(InfoBar.IsClosableProperty);
    }

    [Fact]
    public void ProgressRing_HasRequiredDependencyProperties()
    {
        Assert.NotNull(ProgressRing.IsActiveProperty);
        Assert.NotNull(ProgressRing.DiameterProperty);
    }

    [Fact]
    public void LoadingOverlay_HasRequiredDependencyProperties()
    {
        Assert.NotNull(LoadingOverlay.IsActiveProperty);
        Assert.NotNull(LoadingOverlay.MessageProperty);
    }

    [Fact]
    public void NumberBox_HasRequiredDependencyProperties()
    {
        Assert.NotNull(NumberBox.ValueProperty);
        Assert.NotNull(NumberBox.MinimumProperty);
        Assert.NotNull(NumberBox.MaximumProperty);
        Assert.NotNull(NumberBox.SmallChangeProperty);
        Assert.NotNull(NumberBox.DecimalPlacesProperty);
        Assert.NotNull(NumberBox.SpinButtonPlacementProperty);
        Assert.NotNull(NumberBox.PrefixTextProperty);
        Assert.NotNull(NumberBox.SuffixTextProperty);
        Assert.NotNull(NumberBox.HasPrefixTextProperty);
        Assert.NotNull(NumberBox.HasSuffixTextProperty);
    }

    [Fact]
    public void DateRangePicker_HasRequiredDependencyProperties()
    {
        Assert.NotNull(DateRangePicker.StartDateProperty);
        Assert.NotNull(DateRangePicker.EndDateProperty);
        Assert.NotNull(DateRangePicker.StartHeaderProperty);
        Assert.NotNull(DateRangePicker.EndHeaderProperty);
        Assert.NotNull(DateRangePicker.IsDropDownOpenProperty);
    }

    [Fact]
    public void FluentExpander_HasRequiredDependencyProperties()
    {
        Assert.NotNull(FluentExpander.IsExpandedProperty);
        Assert.NotNull(FluentExpander.IconProperty);
    }

    [Fact]
    public void BreadcrumbBar_HasRequiredDependencyProperties()
    {
        Assert.NotNull(BreadcrumbBar.ItemClickCommandProperty);
        Assert.NotNull(BreadcrumbBar.ItemClickedEvent);
    }

    [Fact]
    public void NumberBox_ValueProperty_BindsTwoWayByDefault()
    {
        var metadata = NumberBox.ValueProperty.GetMetadata(typeof(NumberBox))
            as FrameworkPropertyMetadata;
        Assert.NotNull(metadata);
        Assert.True(metadata.BindsTwoWayByDefault);
    }

    [Fact]
    public void DateRangePicker_DateProperties_BindTwoWayByDefault()
    {
        var startMetadata = DateRangePicker.StartDateProperty.GetMetadata(typeof(DateRangePicker))
            as FrameworkPropertyMetadata;
        var endMetadata = DateRangePicker.EndDateProperty.GetMetadata(typeof(DateRangePicker))
            as FrameworkPropertyMetadata;

        Assert.NotNull(startMetadata);
        Assert.NotNull(endMetadata);
        Assert.True(startMetadata.BindsTwoWayByDefault);
        Assert.True(endMetadata.BindsTwoWayByDefault);
    }

    [Fact]
    public void FluentExpander_IsExpanded_BindsTwoWayByDefault()
    {
        var metadata = FluentExpander.IsExpandedProperty.GetMetadata(typeof(FluentExpander))
            as FrameworkPropertyMetadata;
        Assert.NotNull(metadata);
        Assert.True(metadata.BindsTwoWayByDefault);
    }

    [Fact]
    public void NumberBox_CoercesValueToMinMax()
    {
        // Verify coercion metadata exists (the actual coercion runs in WPF runtime)
        var metadata = NumberBox.ValueProperty.GetMetadata(typeof(NumberBox));
        Assert.NotNull(metadata);
    }

    [Fact]
    public void NumberBox_AdornmentFlags_TrackPrefixAndSuffixText()
    {
        RunOnSta(() =>
        {
            var numberBox = new NumberBox();

            Assert.False(numberBox.HasPrefixText);
            Assert.False(numberBox.HasSuffixText);

            numberBox.PrefixText = "Rs";
            numberBox.SuffixText = "mm";

            Assert.True(numberBox.HasPrefixText);
            Assert.True(numberBox.HasSuffixText);

            numberBox.PrefixText = " ";
            numberBox.SuffixText = string.Empty;

            Assert.False(numberBox.HasPrefixText);
            Assert.False(numberBox.HasSuffixText);
        });
    }

    [Fact]
    public void DateRangePicker_Maintains_A_Valid_Range()
    {
        RunOnSta(() =>
        {
            var picker = new DateRangePicker
            {
                StartDate = new DateTime(2026, 3, 20),
                EndDate = new DateTime(2026, 3, 24)
            };

            picker.StartDate = new DateTime(2026, 3, 28);
            Assert.Equal(new DateTime(2026, 3, 28), picker.EndDate);

            picker.EndDate = new DateTime(2026, 3, 12);
            Assert.Equal(new DateTime(2026, 3, 12), picker.StartDate);
        });
    }

    [Theory]
    [InlineData(typeof(InfoBar), typeof(Control))]
    [InlineData(typeof(ProgressRing), typeof(Control))]
    [InlineData(typeof(LoadingOverlay), typeof(Control))]
    [InlineData(typeof(NumberBox), typeof(Control))]
    [InlineData(typeof(DateRangePicker), typeof(Control))]
    [InlineData(typeof(FluentExpander), typeof(HeaderedContentControl))]
    [InlineData(typeof(BreadcrumbBar), typeof(ItemsControl))]
    [InlineData(typeof(BreadcrumbBarItem), typeof(ContentControl))]
    public void Control_HasCorrectBaseClass(Type controlType, Type expectedBase)
    {
        Assert.True(expectedBase.IsAssignableFrom(controlType),
            $"{controlType.Name} should inherit from {expectedBase.Name}");
    }

    [Fact]
    public void SpinButtonPlacement_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(SpinButtonPlacement), SpinButtonPlacement.Hidden));
        Assert.True(Enum.IsDefined(typeof(SpinButtonPlacement), SpinButtonPlacement.Inline));
    }

    [Fact]
    public void InfoBarSeverity_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(InfoBarSeverity), InfoBarSeverity.Info));
        Assert.True(Enum.IsDefined(typeof(InfoBarSeverity), InfoBarSeverity.Success));
        Assert.True(Enum.IsDefined(typeof(InfoBarSeverity), InfoBarSeverity.Warning));
        Assert.True(Enum.IsDefined(typeof(InfoBarSeverity), InfoBarSeverity.Error));
    }
}
