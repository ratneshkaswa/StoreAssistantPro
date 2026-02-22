using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Validation;

namespace StoreAssistantPro.Tests.Commands;

public class ValidationPipelineBehaviorTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    private sealed record CreateItemCommand(string Name, decimal Price)
        : ICommandRequest<int>;

    private sealed record NoValidatorCommand(int Value)
        : ICommandRequest<bool>;

    // ══════════════════════════════════════════════════════════════
    //  DI builder helper
    // ══════════════════════════════════════════════════════════════

    private sealed class PipelineBuilder
    {
        private readonly ServiceCollection _services = new();

        public PipelineBuilder WithHandler<TCommand, TResult>(
            ICommandRequestHandler<TCommand, TResult> handler)
            where TCommand : ICommandRequest<TResult>
        {
            _services.AddSingleton(handler);
            return this;
        }

        public PipelineBuilder WithValidator<TCommand>(
            ICommandValidator<TCommand> validator)
            where TCommand : ICommand
        {
            _services.AddSingleton(validator);
            return this;
        }

        public PipelineBuilder WithOpenGenericValidationBehavior()
        {
            _services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            _services.AddTransient(
                typeof(ICommandPipelineBehavior<,>),
                typeof(ValidationPipelineBehavior<,>));
            return this;
        }

        public ICommandExecutionPipeline Build()
        {
            var provider = _services.BuildServiceProvider();
            return new CommandExecutionPipeline(
                provider,
                NullLogger<CommandExecutionPipeline>.Instance);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  No validators — passes through
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task NoValidators_PassesThrough_HandlerCalled()
    {
        var handler = new SuccessHandler(42);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task NoValidators_DifferentCommandType_PassesThrough()
    {
        var handler = new BoolHandler(true);
        var pipeline = new PipelineBuilder()
            .WithHandler<NoValidatorCommand, bool>(handler)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<NoValidatorCommand, bool>(
            new NoValidatorCommand(123));

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  Single validator — passes
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SingleValidator_Passes_HandlerCalled()
    {
        var handler = new SuccessHandler(1);
        var validator = new NameRequiredValidator();
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  Single validator — fails → short-circuit
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SingleValidator_Fails_ShortCircuits()
    {
        var handlerCalled = false;
        var handler = new TrackingHandler(() => handlerCalled = true);
        var validator = new NameRequiredValidator();
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("", 9.99m));

        Assert.False(result.Succeeded);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task SingleValidator_Fails_ErrorMessageContainsPropertyName()
    {
        var handler = new SuccessHandler(1);
        var validator = new NameRequiredValidator();
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("", 5m));

        Assert.False(result.Succeeded);
        Assert.Contains("Name", result.ErrorMessage);
        Assert.Contains("required", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Multiple validators — all pass
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultipleValidators_AllPass_HandlerCalled()
    {
        var handler = new SuccessHandler(1);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(new NameRequiredValidator())
            .WithValidator<CreateItemCommand>(new PricePositiveValidator())
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
    }

    // ══════════════════════════════════════════════════════════════
    //  Multiple validators — one fails
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultipleValidators_OneFails_ShortCircuits()
    {
        var handler = new SuccessHandler(1);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(new NameRequiredValidator())
            .WithValidator<CreateItemCommand>(new PricePositiveValidator())
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", -1m));

        Assert.False(result.Succeeded);
        Assert.Contains("Price", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Multiple validators — all fail → errors aggregated
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultipleValidators_AllFail_ErrorsAggregated()
    {
        var handler = new SuccessHandler(1);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(new NameRequiredValidator())
            .WithValidator<CreateItemCommand>(new PricePositiveValidator())
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("", -5m));

        Assert.False(result.Succeeded);
        Assert.Contains("Name", result.ErrorMessage);
        Assert.Contains("Price", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Validator returning multiple errors
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validator_MultipleErrors_AllIncluded()
    {
        var handler = new SuccessHandler(1);
        var validator = new MultiErrorValidator();
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("", -1m));

        Assert.False(result.Succeeded);
        Assert.Contains("Name", result.ErrorMessage);
        Assert.Contains("Price", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Command-level error (empty property name)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validator_CommandLevelError_NoPropertyPrefix()
    {
        var handler = new SuccessHandler(1);
        var validator = new CommandLevelValidator();
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Free", 0m));

        Assert.False(result.Succeeded);
        Assert.Equal("Free items are not allowed.", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  CancellationToken forwarding
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validator_ReceivesCancellationToken()
    {
        CancellationToken? captured = null;
        var validator = new CancellationCapturingValidator(
            ct => captured = ct);
        var handler = new SuccessHandler(1);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithValidator<CreateItemCommand>(validator)
            .WithOpenGenericValidationBehavior()
            .Build();

        using var cts = new CancellationTokenSource();
        await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m), cts.Token);

        Assert.Equal(cts.Token, captured);
    }

    // ══════════════════════════════════════════════════════════════
    //  ValidationResult unit tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidationResult_Success_IsValid()
    {
        var result = ValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public void ValidationResult_WithError_IsInvalid()
    {
        var result = ValidationResult.WithError("Name", "Name is required.");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Name: Name is required.", result.ErrorMessage);
    }

    [Fact]
    public void ValidationResult_WithErrors_AggregatesMessages()
    {
        var result = ValidationResult.WithErrors([
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Price", "Price must be positive.")
        ]);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Name: Name is required.", result.ErrorMessage);
        Assert.Contains("Price: Price must be positive.", result.ErrorMessage);
    }

    [Fact]
    public void ValidationResult_CommandLevelError_NoPropertyPrefix()
    {
        var result = ValidationResult.WithError("", "Command is invalid.");

        Assert.Equal("Command is invalid.", result.ErrorMessage);
    }

    [Fact]
    public void ValidationFailure_PreservesProperties()
    {
        var failure = new ValidationFailure("Price", "Must be positive.");

        Assert.Equal("Price", failure.PropertyName);
        Assert.Equal("Must be positive.", failure.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Test implementations
    // ══════════════════════════════════════════════════════════════

    private sealed class SuccessHandler(int value)
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<int>.Success(value));
    }

    private sealed class BoolHandler(bool value)
        : ICommandRequestHandler<NoValidatorCommand, bool>
    {
        public Task<CommandResult<bool>> HandleAsync(
            NoValidatorCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<bool>.Success(value));
    }

    private sealed class TrackingHandler(Action onCalled)
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default)
        {
            onCalled();
            return Task.FromResult(CommandResult<int>.Success(1));
        }
    }

    private sealed class NameRequiredValidator : ICommandValidator<CreateItemCommand>
    {
        public Task<ValidationResult> ValidateAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(string.IsNullOrWhiteSpace(command.Name)
                ? ValidationResult.WithError("Name", "Name is required.")
                : ValidationResult.Success());
    }

    private sealed class PricePositiveValidator : ICommandValidator<CreateItemCommand>
    {
        public Task<ValidationResult> ValidateAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(command.Price <= 0
                ? ValidationResult.WithError("Price", "Price must be positive.")
                : ValidationResult.Success());
    }

    private sealed class MultiErrorValidator : ICommandValidator<CreateItemCommand>
    {
        public Task<ValidationResult> ValidateAsync(
            CreateItemCommand command, CancellationToken ct = default)
        {
            var errors = new List<ValidationFailure>();

            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add(new("Name", "Name is required."));

            if (command.Price <= 0)
                errors.Add(new("Price", "Price must be positive."));

            return Task.FromResult(errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.WithErrors(errors));
        }
    }

    private sealed class CommandLevelValidator : ICommandValidator<CreateItemCommand>
    {
        public Task<ValidationResult> ValidateAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(command.Price == 0
                ? ValidationResult.WithError("", "Free items are not allowed.")
                : ValidationResult.Success());
    }

    private sealed class CancellationCapturingValidator(
        Action<CancellationToken> capture) : ICommandValidator<CreateItemCommand>
    {
        public Task<ValidationResult> ValidateAsync(
            CreateItemCommand command, CancellationToken ct = default)
        {
            capture(ct);
            return Task.FromResult(ValidationResult.Success());
        }
    }
}
