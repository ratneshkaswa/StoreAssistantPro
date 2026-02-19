using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Commands;

public class SaveProductHandlerTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();

    private SaveProductHandler CreateSut() => new(_productService);

    [Fact]
    public async Task HandleAsync_Success_AddsProduct()
    {
        var result = await CreateSut().HandleAsync(new SaveProductCommand("Widget", 9.99m, 10));

        Assert.True(result.Succeeded);
        await _productService.Received(1).AddAsync(Arg.Is<Product>(p =>
            p.Name == "Widget" && p.Price == 9.99m && p.Quantity == 10));
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        _productService.AddAsync(Arg.Any<Product>())
            .Returns(Task.FromException(new InvalidOperationException("Duplicate name")));

        var result = await CreateSut().HandleAsync(new SaveProductCommand("Dup", 1m, 1));

        Assert.False(result.Succeeded);
        Assert.Equal("Duplicate name", result.ErrorMessage);
    }
}
