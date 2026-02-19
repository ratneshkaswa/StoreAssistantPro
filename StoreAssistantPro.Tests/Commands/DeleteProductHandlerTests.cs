using NSubstitute;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Commands;

public class DeleteProductHandlerTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();

    private DeleteProductHandler CreateSut() => new(_productService);

    [Fact]
    public async Task HandleAsync_Success_ReturnsSuccess()
    {
        var result = await CreateSut().HandleAsync(new DeleteProductCommand(1, [1, 2, 3]));

        Assert.True(result.Succeeded);
        await _productService.Received(1).DeleteAsync(1, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        _productService.DeleteAsync(Arg.Any<int>(), Arg.Any<byte[]?>())
            .Returns(Task.FromException(new InvalidOperationException("Concurrency conflict")));

        var result = await CreateSut().HandleAsync(new DeleteProductCommand(5, null));

        Assert.False(result.Succeeded);
        Assert.Equal("Concurrency conflict", result.ErrorMessage);
    }
}
