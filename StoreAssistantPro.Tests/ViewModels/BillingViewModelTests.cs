using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class BillingViewModelTests
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private BillingViewModel CreateSut()
    {
        _appState.CurrentUserType.Returns(UserType.Admin);
        _regional.FormatCurrency(Arg.Any<decimal>())
            .Returns(call => $"Rs. {call.Arg<decimal>():0.00}");

        return new BillingViewModel(_billingService, _appState, _dialogService, _regional);
    }

    [Fact]
    public async Task CompleteSale_InvalidCashAmount_BlocksSubmission()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });
        sut.PaymentMethod = "Cash";
        sut.CashTendered = "abc";

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid cash amount.", sut.ErrorMessage);
        await _billingService.DidNotReceive().CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void PaymentMethod_TogglesCashAndReferenceState()
    {
        var sut = CreateSut();
        sut.PaymentReference = "UPI 123";
        sut.CashTendered = "2500";

        sut.PaymentMethod = "UPI";

        Assert.False(sut.IsCashPayment);
        Assert.True(sut.RequiresPaymentReference);
        Assert.Equal("0", sut.CashTendered);

        sut.PaymentReference = "UPI 123";
        sut.PaymentMethod = "Cash";

        Assert.True(sut.IsCashPayment);
        Assert.False(sut.RequiresPaymentReference);
        Assert.Empty(sut.PaymentReference);
    }

    [Fact]
    public void CartCommands_Should_Track_Cart_And_Selection_State()
    {
        var sut = CreateSut();

        Assert.False(sut.RemoveCartItemCommand.CanExecute(null));
        Assert.False(sut.ClearCartCommand.CanExecute(null));
        Assert.False(sut.CompleteSaleCommand.CanExecute(null));

        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        Assert.False(sut.RemoveCartItemCommand.CanExecute(null));
        Assert.True(sut.ClearCartCommand.CanExecute(null));
        Assert.True(sut.CompleteSaleCommand.CanExecute(null));

        sut.SelectedCartItem = sut.CartItems.Single();

        Assert.True(sut.RemoveCartItemCommand.CanExecute(null));

        sut.RemoveCartItemCommand.Execute(null);

        Assert.Empty(sut.CartItems);
        Assert.False(sut.RemoveCartItemCommand.CanExecute(null));
        Assert.False(sut.ClearCartCommand.CanExecute(null));
        Assert.False(sut.CompleteSaleCommand.CanExecute(null));
    }

    [Fact]
    public async Task CompleteSale_InvalidDiscountPercentage_BlocksSubmission()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });
        sut.SelectedDiscountType = DiscountType.Percentage;
        sut.DiscountInput = "150";
        sut.PaymentMethod = "Cash";
        sut.CashTendered = "1000";

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Equal("Discount percentage must be between 0 and 100.", sut.ErrorMessage);
        await _billingService.DidNotReceive().CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteSale_NonCashWithoutReference_BlocksSubmission()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });
        sut.PaymentMethod = "UPI";
        sut.PaymentReference = string.Empty;

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Equal("Enter a payment reference for non-cash payments.", sut.ErrorMessage);
        await _billingService.DidNotReceive().CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteSale_InvalidCartLine_BlocksSubmission()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });
        sut.CartItems.Single().Quantity = 0;
        sut.PaymentMethod = "Cash";
        sut.CashTendered = "0";

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Equal("Fix cart lines with invalid quantity, price, or discount.", sut.ErrorMessage);
        await _billingService.DidNotReceive().CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>());
    }
}
