using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Debounces search-box typing so expensive searches run only after the user
/// pauses, while still allowing Enter to trigger an immediate search.
/// </summary>
public static class DebouncedSearch
{
    private sealed class DebouncedSearchState
    {
        public required TextBox TextBox { get; init; }
        public required DispatcherTimer Timer { get; init; }
    }

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(DebouncedSearchState),
            typeof(DebouncedSearch),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DebouncedSearch),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(DebouncedSearch),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DelayProperty =
        DependencyProperty.RegisterAttached(
            "Delay",
            typeof(TimeSpan),
            typeof(DebouncedSearch),
            new PropertyMetadata(TimeSpan.FromMilliseconds(250), OnDelayChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ICommand? GetCommand(DependencyObject obj) => (ICommand?)obj.GetValue(CommandProperty);
    public static void SetCommand(DependencyObject obj, ICommand? value) => obj.SetValue(CommandProperty, value);

    public static TimeSpan GetDelay(DependencyObject obj) => (TimeSpan)obj.GetValue(DelayProperty);
    public static void SetDelay(DependencyObject obj, TimeSpan value) => obj.SetValue(DelayProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if (e.NewValue is true)
        {
            Attach(textBox);
            return;
        }

        Detach(textBox);
    }

    private static void OnDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox ||
            textBox.GetValue(StateProperty) is not DebouncedSearchState state ||
            e.NewValue is not TimeSpan delay)
        {
            return;
        }

        state.Timer.Interval = NormalizeDelay(delay);
    }

    private static void Attach(TextBox textBox)
    {
        if (textBox.GetValue(StateProperty) is DebouncedSearchState)
            return;

        var timer = new DispatcherTimer(DispatcherPriority.Background, textBox.Dispatcher)
        {
            Interval = NormalizeDelay(GetDelay(textBox))
        };

        EventHandler tickHandler = (_, _) =>
        {
            timer.Stop();
            Execute(textBox);
        };
        timer.Tick += tickHandler;

        var state = new DebouncedSearchState
        {
            TextBox = textBox,
            Timer = timer
        };

        textBox.TextChanged += OnTextChanged;
        textBox.PreviewKeyDown += OnPreviewKeyDown;
        textBox.Unloaded += OnUnloaded;
        textBox.SetValue(StateProperty, state);
    }

    private static void Detach(TextBox textBox)
    {
        if (textBox.GetValue(StateProperty) is not DebouncedSearchState state)
            return;

        state.Timer.Stop();
        textBox.TextChanged -= OnTextChanged;
        textBox.PreviewKeyDown -= OnPreviewKeyDown;
        textBox.Unloaded -= OnUnloaded;
        textBox.ClearValue(StateProperty);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.GetValue(StateProperty) is not DebouncedSearchState state)
        {
            return;
        }

        state.Timer.Stop();
        state.Timer.Start();
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.GetValue(StateProperty) is not DebouncedSearchState state)
        {
            return;
        }

        if (e.Key != Key.Enter)
            return;

        state.Timer.Stop();
        Execute(textBox);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            Detach(textBox);
    }

    private static void Execute(TextBox textBox)
    {
        var command = GetCommand(textBox) ?? ResolveSearchCommand(textBox.DataContext);
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }

    private static ICommand? ResolveSearchCommand(object? dataContext)
    {
        if (dataContext is null)
            return null;

        var property = dataContext.GetType().GetProperty(
            "SearchCommand",
            BindingFlags.Instance | BindingFlags.Public);

        return property?.GetValue(dataContext) as ICommand;
    }

    private static TimeSpan NormalizeDelay(TimeSpan delay) =>
        delay <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(250) : delay;
}
