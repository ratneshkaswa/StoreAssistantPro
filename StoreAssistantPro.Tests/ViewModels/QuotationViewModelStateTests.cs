using NSubstitute;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Quotations.Services;
using StoreAssistantPro.Modules.Quotations.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class QuotationViewModelStateTests : IDisposable
{
    private readonly IQuotationService _quotationService = Substitute.For<IQuotationService>();
    private readonly ICustomerService _customerService = Substitute.For<ICustomerService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public QuotationViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Search_Status_And_CurrentPage()
    {
        UserPreferencesStore.SetQuotationsState(new PagedSearchFilterViewState
        {
            SearchText = "QT-19",
            ActiveFilter = "Accepted",
            CurrentPage = 4
        });

        _customerService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Customer>>(Array.Empty<Customer>()));
        _productService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>()));
        _quotationService.ExpireOverdueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _quotationService.GetPagedAsync(
                Arg.Any<PagedQuery>(),
                Arg.Any<string?>(),
                Arg.Any<QuotationStatus?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Quotation>(
                [new Quotation { Id = 19, QuoteNumber = "QT-19", Status = QuotationStatus.Accepted }],
                100,
                4,
                25)));

        var sut = new QuotationViewModel(
            _quotationService,
            _customerService,
            _productService,
            _dialogService,
            _regional);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("QT-19", sut.SearchQuery);
        Assert.Equal(QuotationStatus.Accepted, sut.FilterStatus);
        Assert.Equal(4, sut.CurrentPage);
        await _quotationService.Received(1).ExpireOverdueAsync(Arg.Any<CancellationToken>());
        await _quotationService.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(query => query.Page == 4 && query.PageSize == 25),
            "QT-19",
            QuotationStatus.Accepted,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
