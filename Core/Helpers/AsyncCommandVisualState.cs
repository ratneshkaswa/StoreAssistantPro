using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Mirrors <see cref="IAsyncRelayCommand.IsRunning"/> onto buttons so shared
/// styles can react to async command execution without page-specific bindings.
/// </summary>
public static class AsyncCommandVisualState
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AsyncCommandVisualState),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyPropertyKey IsRunningPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsRunning",
            typeof(bool),
            typeof(AsyncCommandVisualState),
            new PropertyMetadata(false));

    private static readonly DependencyProperty SubscriptionProperty =
        DependencyProperty.RegisterAttached(
            "Subscription",
            typeof(AsyncCommandObserver),
            typeof(AsyncCommandVisualState),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsRunningProperty = IsRunningPropertyKey.DependencyProperty;

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static bool GetIsRunning(DependencyObject obj) => (bool)obj.GetValue(IsRunningProperty);

    private static void SetIsRunning(DependencyObject obj, bool value) => obj.SetValue(IsRunningPropertyKey, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase button)
            return;

        if ((bool)e.NewValue)
        {
            if (button.GetValue(SubscriptionProperty) is not AsyncCommandObserver)
                button.SetValue(SubscriptionProperty, new AsyncCommandObserver(button));

            return;
        }

        if (button.GetValue(SubscriptionProperty) is AsyncCommandObserver observer)
        {
            observer.Dispose();
            button.ClearValue(SubscriptionProperty);
        }

        SetIsRunning(button, false);
    }

    private sealed class AsyncCommandObserver : IDisposable
    {
        private static readonly DependencyPropertyDescriptor? CommandPropertyDescriptor =
            DependencyPropertyDescriptor.FromProperty(ButtonBase.CommandProperty, typeof(ButtonBase));

        private readonly ButtonBase _button;
        private IAsyncRelayCommand? _observedCommand;
        private INotifyPropertyChanged? _commandNotifier;

        public AsyncCommandObserver(ButtonBase button)
        {
            _button = button;
            _button.Loaded += OnButtonLoaded;
            _button.Unloaded += OnButtonUnloaded;
            CommandPropertyDescriptor?.AddValueChanged(_button, OnCommandChanged);
            UpdateObservedCommand();
        }

        private void OnButtonLoaded(object sender, RoutedEventArgs e) => UpdateObservedCommand();

        private void OnButtonUnloaded(object sender, RoutedEventArgs e)
        {
            DetachCommandNotifier();
            SetIsRunning(_button, false);
        }

        private void OnCommandChanged(object? sender, EventArgs e) => UpdateObservedCommand();

        private void UpdateObservedCommand()
        {
            var nextCommand = _button.Command as IAsyncRelayCommand;
            if (ReferenceEquals(nextCommand, _observedCommand))
            {
                UpdateRunningState();
                return;
            }

            DetachCommandNotifier();
            _observedCommand = nextCommand;
            _commandNotifier = _observedCommand as INotifyPropertyChanged;

            if (_commandNotifier is not null)
                _commandNotifier.PropertyChanged += OnCommandPropertyChanged;

            UpdateRunningState();
        }

        private void OnCommandPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) &&
                e.PropertyName != nameof(IAsyncRelayCommand.IsRunning) &&
                e.PropertyName != nameof(IAsyncRelayCommand.ExecutionTask))
            {
                return;
            }

            UpdateRunningState();
        }

        private void UpdateRunningState()
        {
            if (!_button.Dispatcher.CheckAccess())
            {
                _ = _button.Dispatcher.InvokeAsync(UpdateRunningState);
                return;
            }

            SetIsRunning(_button, _observedCommand?.IsRunning == true);
        }

        private void DetachCommandNotifier()
        {
            if (_commandNotifier is not null)
                _commandNotifier.PropertyChanged -= OnCommandPropertyChanged;

            _commandNotifier = null;
            _observedCommand = null;
        }

        public void Dispose()
        {
            _button.Loaded -= OnButtonLoaded;
            _button.Unloaded -= OnButtonUnloaded;
            CommandPropertyDescriptor?.RemoveValueChanged(_button, OnCommandChanged);
            DetachCommandNotifier();
            SetIsRunning(_button, false);
        }
    }
}
