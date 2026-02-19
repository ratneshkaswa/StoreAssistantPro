using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Commands;

public class UpdateProductHandlerTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();

    private UpdateProductHandler CreateSut() => new(_productService);

    [Fact]
    public async Task HandleAsync_ProductExists_UpdatesAndReturnsSuccess()
    {
        var existing = new Product { Id = 1, Name = "Old", Price = 5m, Quantity = 3 };
        _productService.GetByIdAsync(1).Returns(existing);

        var result = await CreateSut().HandleAsync(
            new UpdateProductCommand(1, "New", 10m, 20, [1, 2]));

        Assert.True(result.Succeeded);
        await _productService.Received(1).UpdateAsync(Arg.Is<Product>(p =>
            p.Name == "New" && p.Price == 10m && p.Quantity == 20));
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_ReturnsFailure()
    {
        _productService.GetByIdAsync(99).Returns((Product?)null);

        var result = await CreateSut().HandleAsync(
            new UpdateProductCommand(99, "X", 1m, 1, null));

        Assert.False(result.Succeeded);
        Assert.Equal("Product not found.", result.ErrorMessage);
        await _productService.DidNotReceive().UpdateAsync(Arg.Any<Product>());
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        var existing = new Product { Id = 1, Name = "P", Price = 1m, Quantity = 1 };
        _productService.GetByIdAsync(1).Returns(existing);
        _productService.UpdateAsync(Arg.Any<Product>())
            .Returns(Task.FromException(new InvalidOperationException("Concurrency")));

        var result = await CreateSut().HandleAsync(
            new UpdateProductCommand(1, "P2", 2m, 2, null));

        Assert.False(result.Succeeded);
        Assert.Equal("Concurrency", result.ErrorMessage);
    }
}
