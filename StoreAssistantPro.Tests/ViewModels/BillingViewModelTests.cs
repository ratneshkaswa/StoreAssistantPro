using System.Runtime.CompilerServices;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Tests.ViewModels;

public class BillingViewModelTests
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();
    private readonly ICustomerService _customerService = Substitute.For<ICustomerService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IHeldBillService _heldBillService = Substitute.For<IHeldBillService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private BillingViewModel CreateSut()
    {
        _appState.CurrentUserType.Returns(UserType.Admin);
        _regional.FormatCurrency(Arg.Any<decimal>())
            .Returns(call => $"Rs. {call.Arg<decimal>():0.00}");

        return new BillingViewModel(_billingService, _customerService, _appState, _dialogService, _regional, _heldBillService, _eventBus);
    }

    [Fact]
    public void AddProductToCart_StartsBillingSession()
    {
        var sut = CreateSut();

        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        Received.InOrder(() =>
        {
            _appState.SetBillingSession(BillingSessionState.Active);
        });
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
    public void RemovingLastCartLine_CancelsSession_AndResetsPaymentState()
    {
        var sut = CreateSut();
        sut.PaymentMethod = "UPI";
        sut.PaymentReference = "UPI-123";
        sut.CashTendered = "450";
        sut.SelectedDiscountType = DiscountType.Amount;
        sut.DiscountInput = "25";
        sut.DiscountReason = "carryover";
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        sut.SelectedCartItem = sut.CartItems.Single();
        sut.RemoveCartItemCommand.Execute(null);

        Assert.Empty(sut.CartItems);
        Assert.Equal("Cash", sut.PaymentMethod);
        Assert.Equal(string.Empty, sut.PaymentReference);
        Assert.Equal("0", sut.CashTendered);
        Assert.Equal(DiscountType.None, sut.SelectedDiscountType);
        Assert.Equal("0", sut.DiscountInput);
        Assert.Equal(string.Empty, sut.DiscountReason);
        _appState.Received().SetBillingSession(BillingSessionState.Cancelled);
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

    [Fact]
    public async Task CompleteSale_SetsSessionCompleted_AndClearsCart()
    {
        var sut = CreateSut();
        _billingService.CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>())
            .Returns(new Sale
            {
                InvoiceNumber = "INV-001",
                TotalAmount = 999m
            });

        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });
        sut.PaymentMethod = "Cash";
        sut.CashTendered = "1000";

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        Assert.Empty(sut.CartItems);
        Assert.Equal("Sale INV-001 completed - Rs. 999.00", sut.SuccessMessage);
        _appState.Received().SetBillingSession(BillingSessionState.Completed);
        await _eventBus.Received(1).PublishAsync(Arg.Any<SalesDataChangedEvent>());
    }

    [Fact]
    public void Dispose_ResetsBillingSessionToNone()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        sut.Dispose();

        _appState.Received().SetBillingSession(BillingSessionState.None);
    }

    [Fact]
    public void RemovedCartLine_Should_Not_Keep_BillingViewModel_Alive()
    {
        var (vmRef, removedLine) = CreateRemovedCartLineScenario();

        ForceGarbageCollection();

        Assert.False(vmRef.TryGetTarget(out _));
        GC.KeepAlive(removedLine);
    }

    [Fact]
    public void Dispose_Should_Detach_TrackedCartLines()
    {
        var (vmRef, trackedLine) = CreateDisposedCartTrackingScenario();

        ForceGarbageCollection();

        Assert.False(vmRef.TryGetTarget(out _));
        GC.KeepAlive(trackedLine);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (WeakReference<BillingViewModel> VmRef, CartLineViewModel Line) CreateRemovedCartLineScenario()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        var removedLine = sut.CartItems.Single();
        sut.SelectedCartItem = removedLine;
        sut.RemoveCartItemCommand.Execute(null);

        return (new WeakReference<BillingViewModel>(sut), removedLine);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (WeakReference<BillingViewModel> VmRef, CartLineViewModel Line) CreateDisposedCartTrackingScenario()
    {
        var sut = CreateSut();
        sut.AddProductToCartCommand.Execute(new Product { Id = 1, Name = "Shirt", SalePrice = 999m });

        var trackedLine = sut.CartItems.Single();
        sut.Dispose();

        return (new WeakReference<BillingViewModel>(sut), trackedLine);
    }

    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
