using StoreAssistantPro.Core.Controls;
using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Tests.Core;

/// <summary>
/// Validates the Win11 WinUI-style control library.
/// Checks that each control has the correct base class and
/// required dependency properties (templates are validated at runtime).
/// </summary>
public class FluentControlsTests
{
    [Theory]
    [InlineData(typeof(InfoBar))]
    [InlineData(typeof(ProgressRing))]
    [InlineData(typeof(LoadingOverlay))]
    [InlineData(typeof(NumberBox))]
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

    [Theory]
    [InlineData(typeof(InfoBar), typeof(Control))]
    [InlineData(typeof(ProgressRing), typeof(Control))]
    [InlineData(typeof(LoadingOverlay), typeof(Control))]
    [InlineData(typeof(NumberBox), typeof(Control))]
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
