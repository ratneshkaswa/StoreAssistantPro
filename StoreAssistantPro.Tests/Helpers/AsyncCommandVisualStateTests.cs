using System.ComponentModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class AsyncCommandVisualStateTests
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
    public void AsyncRelayCommand_Execution_TogglesRunningState()
    {
        RunOnSta(() =>
        {
            var command = new TestAsyncCommand();
            var button = new Button { Command = command };

            AsyncCommandVisualState.SetIsEnabled(button, true);

            Assert.False(AsyncCommandVisualState.GetIsRunning(button));

            var execution = command.ExecuteAsync(null);

            Assert.True(AsyncCommandVisualState.GetIsRunning(button));

            command.Complete();
            execution.GetAwaiter().GetResult();

            Assert.False(AsyncCommandVisualState.GetIsRunning(button));
        });
    }

    [Fact]
    public void ChangingCommand_ClearsPreviousRunningState()
    {
        RunOnSta(() =>
        {
            var asyncCommand = new TestAsyncCommand();
            var syncCommand = new RelayCommand(() => { });
            var button = new Button { Command = asyncCommand };

            AsyncCommandVisualState.SetIsEnabled(button, true);

            var execution = asyncCommand.ExecuteAsync(null);

            Assert.True(AsyncCommandVisualState.GetIsRunning(button));

            button.Command = syncCommand;

            Assert.False(AsyncCommandVisualState.GetIsRunning(button));

            asyncCommand.Complete();
            execution.GetAwaiter().GetResult();
        });
    }

    private sealed class TestAsyncCommand : IAsyncRelayCommand
    {
        private readonly TaskCompletionSource<object?> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? CanExecuteChanged;

        public Task? ExecutionTask { get; private set; }

        public bool CanBeCanceled => false;

        public bool IsCancellationRequested => false;

        public bool IsRunning { get; private set; }

        public bool CanExecute(object? parameter) => !IsRunning;

        public void Execute(object? parameter) => _ = ExecuteAsync(parameter);

        public Task ExecuteAsync(object? parameter)
        {
            if (ExecutionTask is null)
            {
                IsRunning = true;
                ExecutionTask = _completion.Task;
                RaiseStateChanged();
            }

            return ExecutionTask;
        }

        public void Cancel()
        {
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public void Complete()
        {
            IsRunning = false;
            RaiseStateChanged();
            _completion.TrySetResult(null);
        }

        private void RaiseStateChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutionTask)));
            NotifyCanExecuteChanged();
        }
    }
}
