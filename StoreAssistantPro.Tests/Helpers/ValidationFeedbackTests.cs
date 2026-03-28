using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class ValidationFeedbackTests
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

    [Fact]
    public void ShakeOnError_FirstValidationFailure_AddsTranslateTransform()
    {
        RunOnSta(() =>
        {
            var textBox = CreateInvalidatingTextBox();

            ValidationFeedback.SetShakeOnError(textBox, true);
            TriggerValidationError(textBox);

            Assert.True(Validation.GetHasError(textBox));
            Assert.NotNull(FindTranslateTransform(textBox.RenderTransform));
        });
    }

    [Fact]
    public void ShakeOnError_PreservesExistingRenderTransform()
    {
        RunOnSta(() =>
        {
            var textBox = CreateInvalidatingTextBox();
            var scale = new ScaleTransform(1.1, 1.1);
            textBox.RenderTransform = scale;

            ValidationFeedback.SetShakeOnError(textBox, true);
            TriggerValidationError(textBox);

            var group = Assert.IsType<TransformGroup>(textBox.RenderTransform);
            Assert.Same(scale, group.Children[0]);
            Assert.Contains(group.Children, transform => transform is TranslateTransform);
        });
    }

    [Fact]
    public void ValidationFeedback_Should_Avoid_Shake_Storyboard_Animation()
    {
        var source = File.ReadAllText(
            Path.Combine(FindSolutionRoot(), "Core", "Helpers", "ValidationFeedback.cs"));

        Assert.DoesNotContain("DoubleAnimationUsingKeyFrames", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EasingDoubleKeyFrame", source, StringComparison.Ordinal);
        Assert.Contains("translate.X = translate.X;", source, StringComparison.Ordinal);
    }

    private static TextBox CreateInvalidatingTextBox()
    {
        var textBox = new TextBox();
        var binding = new Binding(nameof(TestInput.Value))
        {
            Source = new TestInput(),
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit
        };
        binding.ValidationRules.Add(new AlwaysInvalidRule());

        BindingOperations.SetBinding(textBox, TextBox.TextProperty, binding);
        return textBox;
    }

    private static void TriggerValidationError(TextBox textBox)
    {
        textBox.Text = "bad";
        var expression = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
        Assert.NotNull(expression);
        expression!.UpdateSource();
    }

    private static TranslateTransform? FindTranslateTransform(Transform? transform) =>
        transform switch
        {
            TranslateTransform translate => translate,
            TransformGroup group => group.Children.OfType<TranslateTransform>().FirstOrDefault(),
            _ => null
        };

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 ||
                Directory.GetFiles(dir, "*.slnx").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }

    private sealed class TestInput
    {
        public string? Value { get; set; }
    }

    private sealed class AlwaysInvalidRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) =>
            new(false, "Invalid");
    }
}
