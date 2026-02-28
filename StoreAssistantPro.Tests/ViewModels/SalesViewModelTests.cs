using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Services;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class SalesViewModelTests
{
    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IBillCalculationService _billCalculation = new BillCalculationService();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public SalesViewModelTests()
    {
        _sessionService.CurrentUserType.Returns(UserType.Admin);
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(CommandResult.Success());
        _regional.Now.Returns(new DateTime(2026, 1, 15, 10, 30, 0));
    }

    private SalesViewModel CreateSut() =>
        new(_salesService, _productService, _sessionService, _commandBus, _billCalculation, _regional);

    private void SetupPagedReturn(IReadOnlyList<Sale> items, int totalCount = -1)
    {
        var count = totalCount < 0 ? items.Count : totalCount;
        _salesService.GetPagedAsync(
                Arg.Any<PagedQuery>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var query = ci.Arg<PagedQuery>();
                return new PagedResult<Sale>(items, count, query.PageIndex, query.PageSize);
            });
    }

    [Fact]
    public async Task LoadSales_PopulatesSalesList()
    {
        var sales = new List<Sale>
        {
            new() { Id = 1, TotalAmount = 50m, PaymentMethod = "Cash", Items = [] },
            new() { Id = 2, TotalAmount = 75m, PaymentMethod = "Card", Items = [] }
        };
        SetupPagedReturn(sales);

        var sut = CreateSut();
        await sut.LoadSalesCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Sales.Count);
    }

    [Fact]
    public async Task LoadSales_UpdatesPagingState()
    {
        var sales = new List<Sale>
        {
            new() { Id = 1, TotalAmount = 50m, PaymentMethod = "Cash", Items = [] }
        };
        SetupPagedReturn(sales, totalCount: 75);

        var sut = CreateSut();
        await sut.LoadSalesCommand.ExecuteAsync(null);

        Assert.Equal(75, sut.TotalCount);
        Assert.Equal(2, sut.TotalPages);
        Assert.Equal(0, sut.PageIndex);
        Assert.True(sut.HasNextPage);
        Assert.False(sut.HasPreviousPage);
    }

    [Fact]
    public async Task LoadSales_PassesNullDates_WhenNotFiltered()
    {
        SetupPagedReturn([]);

        var sut = CreateSut();
        await sut.LoadSalesCommand.ExecuteAsync(null);

        await _salesService.Received(1).GetPagedAsync(
            Arg.Any<PagedQuery>(),
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterByDate_CallsServiceWithDateRange()
    {
        SetupPagedReturn([]);

        var sut = CreateSut();
        sut.FilterFrom = new DateTime(2026, 1, 1);
        sut.FilterTo = new DateTime(2026, 1, 31);

        await sut.FilterByDateCommand.ExecuteAsync(null);

        await _salesService.Received(1).GetPagedAsync(
            Arg.Any<PagedQuery>(),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddToCart_AddsItemFromSelectedProduct()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 3;

        sut.AddToCartCommand.Execute(null);

        Assert.Single(sut.CartItems);
        Assert.Equal(1, sut.CartItems[0].ProductId);
        Assert.Equal(3, sut.CartItems[0].Quantity);
        Assert.Equal(10m, sut.CartItems[0].UnitPrice);
        Assert.Equal(30m, sut.CartTotal);
    }

    [Fact]
    public void AddToCart_WithNoSelection_DoesNothing()
    {
        var sut = CreateSut();
        sut.SelectedProduct = null;
        sut.CartQuantity = 1;

        sut.AddToCartCommand.Execute(null);

        Assert.Empty(sut.CartItems);
    }

    [Fact]
    public void AddToCart_ExceedingStock_SetsErrorMessage()
    {
        var product = new Product { Id = 1, Name = "Limited", SalePrice = 10m, Quantity = 2 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 5;

        sut.AddToCartCommand.Execute(null);

        Assert.Contains("Only 2 available", sut.ErrorMessage);
        Assert.Empty(sut.CartItems);
    }

    [Fact]
    public void AddToCart_SameProductTwice_IncrementsQuantity()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedProduct = product;
        sut.CartQuantity = 3;
        sut.AddToCartCommand.Execute(null);

        Assert.Single(sut.CartItems);
        Assert.Equal(5, sut.CartItems[0].Quantity);
        Assert.Equal(50m, sut.CartTotal);
    }

    [Fact]
    public void RemoveFromCart_RemovesItemAndUpdatesTotal()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        var cartItem = sut.CartItems[0];
        sut.RemoveFromCartCommand.Execute(cartItem);

        Assert.Empty(sut.CartItems);
        Assert.Equal(0m, sut.CartTotal);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyCart_DoesNotCallService()
    {
        var sut = CreateSut();
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.DidNotReceive().SendAsync(Arg.Any<CompleteSaleCommand>());
    }

    [Fact]
    public async Task CompleteSale_CreatesAndReloads()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.PaymentMethod = "Card";
        sut.AddToCartCommand.Execute(null);

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteSaleCommand>(c =>
            c.TotalAmount == 20m &&
            c.PaymentMethod == "Card" &&
            c.Items.Count == 1));
        Assert.False(sut.IsNewSaleVisible);
    }

    [Fact]
    public async Task ShowNewSale_LoadsAvailableProductsAndResetsForm()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "InStock", SalePrice = 10m, Quantity = 5 },
            new() { Id = 2, Name = "OutOfStock", SalePrice = 20m, Quantity = 0 }
        };
        _productService.GetAllAsync().Returns(products);

        var sut = CreateSut();
        await sut.ShowNewSaleCommand.ExecuteAsync(null);

        Assert.True(sut.IsNewSaleVisible);
        Assert.Single(sut.AvailableProducts);
        Assert.Equal("InStock", sut.AvailableProducts[0].Name);
        Assert.Empty(sut.CartItems);
        Assert.Equal(0m, sut.CartTotal);
        Assert.Equal("Cash", sut.PaymentMethod);
    }

    [Fact]
    public void CancelNewSale_HidesForm()
    {
        var sut = CreateSut();
        sut.IsNewSaleVisible = true;

        sut.CancelNewSaleCommand.Execute(null);

        Assert.False(sut.IsNewSaleVisible);
    }

    // ── Role-based access tests ──

    [Fact]
    public async Task ShowNewSale_AsUser_SetsErrorMessage()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();

        await sut.ShowNewSaleCommand.ExecuteAsync(null);

        Assert.False(sut.IsNewSaleVisible);
        Assert.Contains("administrators and managers", sut.ErrorMessage);
    }

    [Fact]
    public void CanCreateSales_Manager_ReturnsTrue()
    {
        _sessionService.CurrentUserType.Returns(UserType.Manager);
        var sut = CreateSut();
        Assert.True(sut.CanCreateSales);
    }

    [Fact]
    public void CanCreateSales_User_ReturnsFalse()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();
        Assert.False(sut.CanCreateSales);
    }

    // ── Sale detail tests ──

    [Fact]
    public void HasSelectedSale_WithSelection_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.SelectedSale = new Sale { Id = 1, TotalAmount = 50m, PaymentMethod = "Cash", Items = [] };
        Assert.True(sut.HasSelectedSale);
    }

    [Fact]
    public void HasSelectedSale_WithoutSelection_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.SelectedSale = null;
        Assert.False(sut.HasSelectedSale);
    }

    // ── Bill-level discount tests ──

    [Fact]
    public void AddToCart_RecalculatesBillFinalAmount()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        Assert.Equal(200m, sut.CartTotal);
        Assert.Equal(200m, sut.BillFinalAmount);
        Assert.Equal(0m, sut.BillDiscountAmount);
    }

    [Fact]
    public void Discount_None_FinalEqualsSubtotal()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 5;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedDiscountType = DiscountType.None;
        sut.DiscountInput = 999m;

        Assert.Equal(500m, sut.BillFinalAmount);
        Assert.Equal(0m, sut.BillDiscountAmount);
    }

    [Fact]
    public void Discount_Amount_SubtractedFromTotal()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 5;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedDiscountType = DiscountType.Amount;
        sut.DiscountInput = 50m;

        Assert.Equal(50m, sut.BillDiscountAmount);
        Assert.Equal(450m, sut.BillFinalAmount);
    }

    [Fact]
    public void Discount_Percentage_CalculatesCorrectly()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 10;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedDiscountType = DiscountType.Percentage;
        sut.DiscountInput = 10m; // 10%

        Assert.Equal(100m, sut.BillDiscountAmount);
        Assert.Equal(900m, sut.BillFinalAmount);
    }

    [Fact]
    public void Discount_ChangingType_RecalculatesInstantly()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 200m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);

        sut.DiscountInput = 20m;
        sut.SelectedDiscountType = DiscountType.Amount;
        Assert.Equal(180m, sut.BillFinalAmount);

        sut.SelectedDiscountType = DiscountType.Percentage;
        Assert.Equal(160m, sut.BillFinalAmount); // 200 - 20% = 160
    }

    [Fact]
    public void Discount_RemoveFromCart_RecalculatesWithDiscount()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 3;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedDiscountType = DiscountType.Amount;
        sut.DiscountInput = 50m;
        Assert.Equal(250m, sut.BillFinalAmount);

        sut.RemoveFromCartCommand.Execute(sut.CartItems[0]);
        Assert.Equal(0m, sut.BillFinalAmount);
        Assert.Equal(0m, sut.BillDiscountAmount);
    }

    [Fact]
    public async Task ShowNewSale_ResetsDiscountFields()
    {
        _productService.GetAllAsync().Returns(new List<Product>());

        var sut = CreateSut();
        sut.SelectedDiscountType = DiscountType.Percentage;
        sut.DiscountInput = 15m;
        sut.DiscountReason = "Loyalty";

        await sut.ShowNewSaleCommand.ExecuteAsync(null);

        Assert.Equal(DiscountType.None, sut.SelectedDiscountType);
        Assert.Equal(0m, sut.DiscountInput);
        Assert.Equal(string.Empty, sut.DiscountReason);
    }

    [Fact]
    public async Task CompleteSale_WithDiscount_PassesDiscountToCommand()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 100m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedDiscountType = DiscountType.Amount;
        sut.DiscountInput = 30m;
        sut.DiscountReason = "VIP";
        sut.PaymentMethod = "Cash";

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteSaleCommand>(c =>
            c.TotalAmount == 170m &&
            c.Discount.Type == DiscountType.Amount &&
            c.Discount.Value == 30m &&
            c.Discount.Reason == "VIP"));
    }

    [Fact]
    public async Task CompleteSale_NoDiscount_PassesDiscountNone()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 50m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteSaleCommand>(c =>
            c.TotalAmount == 50m &&
            c.Discount.Type == DiscountType.None));
    }

    [Fact]
    public async Task CompleteSale_GeneratesNonEmptyIdempotencyKey()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteSaleCommand>(c =>
            c.IdempotencyKey != Guid.Empty));
    }

    [Fact]
    public async Task CompleteSale_TwoCallsGenerateDifferentKeys()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var capturedKeys = new List<Guid>();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                capturedKeys.Add(ci.Arg<CompleteSaleCommand>().IdempotencyKey);
                return CommandResult.Success();
            });

        var sut = CreateSut();

        // First sale
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        // Second sale (re-add to cart since form resets)
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Equal(2, capturedKeys.Count);
        Assert.NotEqual(capturedKeys[0], capturedKeys[1]);
    }

    [Fact]
    public async Task CompleteSale_IsSavingTrueDuringSave()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        bool wasSavingDuringCall = false;
        var sut = CreateSut();

        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                wasSavingDuringCall = sut.IsSaving;
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.True(wasSavingDuringCall);
        Assert.False(sut.IsSaving); // reset after completion
    }

    [Fact]
    public async Task CompleteSale_IsSavingResetOnFailure()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(CommandResult.Failure("boom"));

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.False(sut.IsSaving);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // ── UI protection during billing transaction ──

    [Fact]
    public void IsCartLocked_WhenNotSaving_ReturnsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.IsCartLocked);
    }

    [Fact]
    public async Task IsCartLocked_DuringSave_ReturnsTrue()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };
        bool wasLockedDuringSave = false;

        var sut = CreateSut();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                wasLockedDuringSave = sut.IsCartLocked;
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.True(wasLockedDuringSave);
        Assert.False(sut.IsCartLocked);
    }

    [Fact]
    public async Task SavingStatusText_DuringSave_ShowsProcessing()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };
        string? statusDuringSave = null;

        var sut = CreateSut();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                statusDuringSave = sut.SavingStatusText;
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrEmpty(statusDuringSave));
        Assert.Equal(string.Empty, sut.SavingStatusText);
    }

    [Fact]
    public async Task AddToCartCommand_DisabledDuringSave()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };
        bool canAddDuringSave = true;

        var sut = CreateSut();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                canAddDuringSave = sut.AddToCartCommand.CanExecute(null);
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.False(canAddDuringSave);
    }

    [Fact]
    public async Task CancelNewSaleCommand_DisabledDuringSave()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };
        bool canCancelDuringSave = true;

        var sut = CreateSut();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                canCancelDuringSave = sut.CancelNewSaleCommand.CanExecute(null);
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.False(canCancelDuringSave);
    }

    [Fact]
    public async Task ShowNewSaleCommand_DisabledDuringSave()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };
        bool canShowNewSaleDuringSave = true;

        var sut = CreateSut();
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(ci =>
            {
                canShowNewSaleDuringSave = sut.ShowNewSaleCommand.CanExecute(null);
                return CommandResult.Success();
            });

        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.False(canShowNewSaleDuringSave);
    }

    [Fact]
    public async Task AllCartCommands_ReenabledAfterSave()
    {
        SetupPagedReturn([]);
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.True(sut.AddToCartCommand.CanExecute(null));
        Assert.True(sut.CancelNewSaleCommand.CanExecute(null));
        Assert.True(sut.ShowNewSaleCommand.CanExecute(null));
        Assert.True(sut.CompleteSaleCommand.CanExecute(null));
    }

    [Fact]
    public async Task AllCartCommands_ReenabledAfterFailure()
    {
        var product = new Product { Id = 1, Name = "Widget", SalePrice = 10m, Quantity = 50 };

        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(CommandResult.Failure("fail"));

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 1;
        sut.AddToCartCommand.Execute(null);
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.True(sut.AddToCartCommand.CanExecute(null));
        Assert.True(sut.CancelNewSaleCommand.CanExecute(null));
        Assert.True(sut.ShowNewSaleCommand.CanExecute(null));
        Assert.True(sut.CompleteSaleCommand.CanExecute(null));
        Assert.False(sut.IsSaving);
    }
}
