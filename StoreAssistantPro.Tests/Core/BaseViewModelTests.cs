using StoreAssistantPro.Core;

namespace StoreAssistantPro.Tests.Core;

public class BaseViewModelTests
{
    internal sealed partial class TestViewModel : BaseViewModel
    {
        public override string Title => "Custom Title";
        public Task TestRunAsync(Func<CancellationToken, Task> action) => RunAsync(action);
        public Task<T?> TestRunAsync<T>(Func<CancellationToken, Task<T>> action) => RunAsync(action);
        public Task TestRunLoadAsync(Func<CancellationToken, Task> action) => RunLoadAsync(action);
    }

    internal sealed partial class DefaultTitleViewModel : BaseViewModel;

    [Fact]
    public void Title_Default_StripsViewModelSuffix()
    {
        var sut = new DefaultTitleViewModel();

        Assert.Equal("DefaultTitle", sut.Title);
    }

    [Fact]
    public void Title_Override_ReturnsCustom()
    {
        var sut = new TestViewModel();

        Assert.Equal("Custom Title", sut.Title);
    }

    [Fact]
    public void ErrorMessage_DefaultsToEmpty()
    {
        var sut = new TestViewModel();

        Assert.Equal(string.Empty, sut.ErrorMessage);
    }

    [Fact]
    public void LoadErrorMessage_DefaultsToEmpty()
    {
        var sut = new TestViewModel();

        Assert.Equal(string.Empty, sut.LoadErrorMessage);
        Assert.False(sut.HasLoadError);
    }

    [Fact]
    public void IsLoading_DefaultsToFalse()
    {
        var sut = new TestViewModel();

        Assert.False(sut.IsLoading);
    }

    [Fact]
    public void IsBusy_DefaultsToFalse()
    {
        var sut = new TestViewModel();

        Assert.False(sut.IsBusy);
    }

    [Fact]
    public void ClearState_ResetsAllTransientProperties()
    {
        var sut = new TestViewModel
        {
            ErrorMessage = "some error",
            LoadErrorMessage = "load error",
            IsBusy = true,
            IsLoading = true
        };

        sut.ClearState();

        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.Equal(string.Empty, sut.LoadErrorMessage);
        Assert.False(sut.IsBusy);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RunAsync_Success_SetsAndClearsIsBusy()
    {
        var sut = new TestViewModel();
        var wasBusyDuring = false;

        await sut.TestRunAsync(ct =>
        {
            wasBusyDuring = sut.IsBusy;
            return Task.CompletedTask;
        });

        Assert.True(wasBusyDuring);
        Assert.False(sut.IsBusy);
        Assert.Equal(string.Empty, sut.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_Failure_CapturesErrorMessage()
    {
        var sut = new TestViewModel();

        await sut.TestRunAsync(ct => throw new InvalidOperationException("boom"));

        Assert.Equal("boom", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RunAsyncT_Success_ReturnsValue()
    {
        var sut = new TestViewModel();

        var result = await sut.TestRunAsync(ct => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(string.Empty, sut.ErrorMessage);
    }

    [Fact]
    public async Task RunAsyncT_Failure_ReturnsDefault()
    {
        var sut = new TestViewModel();

        var result = await sut.TestRunAsync<int>(ct =>
            throw new InvalidOperationException("fail"));

        Assert.Equal(0, result);
        Assert.Equal("fail", sut.ErrorMessage);
    }

    [Fact]
    public void PropertyChanged_Fires_ForErrorMessage()
    {
        var sut = new TestViewModel();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.ErrorMessage = "test";

        Assert.Contains(nameof(BaseViewModel.ErrorMessage), changed);
    }

    [Fact]
    public void PropertyChanged_Fires_ForIsLoading()
    {
        var sut = new TestViewModel();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.IsLoading = true;

        Assert.Contains(nameof(BaseViewModel.IsLoading), changed);
    }

    [Fact]
    public void PropertyChanged_Fires_ForIsBusy()
    {
        var sut = new TestViewModel();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.IsBusy = true;

        Assert.Contains(nameof(BaseViewModel.IsBusy), changed);
    }

    [Fact]
    public async Task RunLoadAsync_SetsAndClearsIsLoading()
    {
        var sut = new TestViewModel();
        var wasLoadingDuring = false;

        await sut.TestRunLoadAsync(ct =>
        {
            wasLoadingDuring = sut.IsLoading;
            return Task.CompletedTask;
        });

        Assert.True(wasLoadingDuring);
        Assert.False(sut.IsLoading);
        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.Equal(string.Empty, sut.LoadErrorMessage);
    }

    [Fact]
    public async Task RunLoadAsync_Failure_CapturesErrorMessage()
    {
        var sut = new TestViewModel();

        await sut.TestRunLoadAsync(ct => throw new InvalidOperationException("load fail"));

        Assert.Equal("load fail", sut.ErrorMessage);
        Assert.Equal("load fail", sut.LoadErrorMessage);
        Assert.True(sut.HasLoadError);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RunLoadAsync_Cancellation_SwallowsAndClearsLoading()
    {
        var sut = new TestViewModel();

        await sut.TestRunLoadAsync(ct => throw new OperationCanceledException());

        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.Equal(string.Empty, sut.LoadErrorMessage);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RunAsync_Cancellation_SwallowsAndClearsBusy()
    {
        var sut = new TestViewModel();

        await sut.TestRunAsync(ct => throw new OperationCanceledException());

        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RunAsync_ReentrantCall_IsIgnored()
    {
        var sut = new TestViewModel();
        var callCount = 0;
        var tcs = new TaskCompletionSource();

        var first = sut.TestRunAsync(async ct =>
        {
            Interlocked.Increment(ref callCount);
            await tcs.Task;
        });

        // While first is still running, trigger another.
        var second = sut.TestRunAsync(ct =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        tcs.SetResult();
        await first;
        await second;

        Assert.Equal(1, callCount);
    }
}
