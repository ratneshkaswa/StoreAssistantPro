using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Printing;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Billing.Views;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Products.Views;
using StoreAssistantPro.Modules.Payments.Services;
using StoreAssistantPro.Modules.Payments.ViewModels;
using StoreAssistantPro.Modules.Payments.Views;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Modules.Reports.ViewModels;
using StoreAssistantPro.Modules.Reports.Views;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;
using StoreAssistantPro.Modules.Settings.Views;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("UserPreferences")]
public sealed class ViewSnapshotBaselineTests : IDisposable
{
    public ViewSnapshotBaselineTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public void WorkspaceView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var dashboardService = Substitute.For<IDashboardService>();
            dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>())
                .Returns(DashboardSummary.Empty);

            var regional = Substitute.For<IRegionalSettingsService>();
            regional.Now.Returns(new DateTime(2026, 3, 25, 9, 30, 0));
            regional.CurrencySymbol.Returns("Rs.");
            regional.FormatDate(Arg.Any<DateTime>())
                .Returns(call => ((DateTime)call[0]).ToString("dd-MMM-yyyy"));

            var eventBus = Substitute.For<IEventBus>();
            var viewModel = new WorkspaceViewModel(dashboardService, regional, eventBus);
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            var view = new WorkspaceView
            {
                DataContext = viewModel,
                Width = 1280,
                Height = 900
            };

            ViewSnapshotVerifier.AssertMatches("WorkspaceView", view, 1280, 900);
        });
    }

    [Fact]
    public void SystemSettingsView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var settingsService = Substitute.For<ISystemSettingsService>();
            settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings
            {
                BackupLocation = "C:\\Backups",
                BackupTime = "09:30",
                DefaultPrinter = "Counter Printer",
                PrinterWidth = 58,
                DefaultPageSize = "Thermal",
                AutoLogoutMinutes = 15
            });

            var viewModel = new SystemSettingsViewModel(
                settingsService,
                Substitute.For<ILoginService>(),
                Substitute.For<IDialogService>(),
                Substitute.For<IUiDensityService>(),
                Substitute.For<IEventBus>());
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            var view = new SystemSettingsView
            {
                DataContext = viewModel,
                Width = 1320,
                Height = 940
            };

            ViewSnapshotVerifier.AssertMatches("SystemSettingsView", view, 1320, 940);
        });
    }

    [Fact]
    public void SaleHistoryView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var historyService = Substitute.For<ISaleHistoryService>();
            var receiptService = Substitute.For<IReceiptService>();
            var regional = Substitute.For<IRegionalSettingsService>();

            regional.FormatDate(Arg.Any<DateTime>())
                .Returns(call => ((DateTime)call[0]).ToString("dd-MMM-yyyy"));
            regional.FormatTime(Arg.Any<DateTime>())
                .Returns(call => ((DateTime)call[0]).ToString("hh:mm tt"));
            regional.FormatCurrency(Arg.Any<decimal>())
                .Returns(call => $"Rs.{((decimal)call[0]):N0}");

            var firstSale = new Sale
            {
                Id = 42,
                InvoiceNumber = "INV-2042",
                SaleDate = new DateTime(2026, 3, 25, 10, 45, 0),
                TotalAmount = 2450,
                PaymentMethod = "UPI",
                PaymentReference = "TXN-88421",
                DiscountAmount = 100,
                Items =
                [
                    new SaleItem { Quantity = 2, Product = new Product { Name = "Oxford Shirt" } },
                    new SaleItem { Quantity = 1, Product = new Product { Name = "Formal Trouser" } }
                ]
            };

            var secondSale = new Sale
            {
                Id = 43,
                InvoiceNumber = "INV-2043",
                SaleDate = new DateTime(2026, 3, 25, 12, 10, 0),
                TotalAmount = 1180,
                PaymentMethod = "Cash",
                Items =
                [
                    new SaleItem { Quantity = 1, Product = new Product { Name = "Polo Tee" } }
                ]
            };

            historyService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(new PagedResult<Sale>([firstSale, secondSale], 2, 1, 25));

            var eventBus2 = Substitute.For<IEventBus>();
            var viewModel = new SaleHistoryViewModel(historyService, receiptService, regional, eventBus2);
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            viewModel.DateFrom = new DateTime(2026, 3, 1);
            viewModel.DateTo = new DateTime(2026, 3, 31);
            viewModel.InvoiceSearch = "INV";
            viewModel.SelectedSale = firstSale;
            viewModel.ReceiptPreview = "STORE ASSISTANT PRO\nINV-2042\nOxford Shirt x2\nFormal Trouser x1\nTOTAL Rs.2,450";

            var view = new SaleHistoryView
            {
                DataContext = viewModel,
                Width = 1380,
                Height = 920
            };

            ViewSnapshotVerifier.AssertMatches("SaleHistoryView", view, 1380, 920);
        });
    }

    [Fact]
    public void ProductManagementView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var productService = Substitute.For<IProductService>();
            var taxGroupService = Substitute.For<ITaxGroupService>();
            var regional = Substitute.For<IRegionalSettingsService>();

            regional.CurrencySymbol.Returns("Rs.");

            var category = new Category { Id = 4, Name = "Shirts" };
            var altCategory = new Category { Id = 5, Name = "Trousers" };
            var brand = new Brand { Id = 6, Name = "Acme" };
            var altBrand = new Brand { Id = 7, Name = "Northwind" };
            var vendor = new Vendor { Id = 9, Name = "Textiles Co" };
            var tax = new TaxMaster { Id = 2, TaxName = "GST 5%", SlabPercent = 5 };
            var taxGroup = new TaxGroup { Id = 3, Name = "Apparel GST" };
            var hsnCode = new HSNCode { Id = 8, Code = "6205", Description = "Shirts", Category = HSNCategory.Garments };

            var firstProduct = new Product
            {
                Id = 10,
                Name = "Oxford Shirt",
                ProductType = ProductType.Readymade,
                Unit = ProductUnit.Piece,
                CategoryId = category.Id,
                Category = category,
                BrandId = brand.Id,
                Brand = brand,
                VendorId = vendor.Id,
                Vendor = vendor,
                TaxId = tax.Id,
                Tax = tax,
                Quantity = 12,
                MinStockLevel = 5,
                SalePrice = 1499,
                CostPrice = 875,
                Barcode = "8901234567890",
                IsTaxInclusive = true,
                SupportsColour = true,
                SupportsSize = true
            };

            var secondProduct = new Product
            {
                Id = 11,
                Name = "Pleated Trouser",
                ProductType = ProductType.Readymade,
                Unit = ProductUnit.Piece,
                CategoryId = altCategory.Id,
                Category = altCategory,
                BrandId = altBrand.Id,
                Brand = altBrand,
                Quantity = 3,
                MinStockLevel = 5,
                SalePrice = 1899,
                CostPrice = 1125,
                Barcode = "8900000000001"
            };

            productService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Product>>([firstProduct, secondProduct]));
            productService.GetActiveTaxesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<TaxMaster>>([tax]));
            productService.GetActiveCategoriesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Category>>([category, altCategory]));
            productService.GetActiveBrandsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Brand>>([brand, altBrand]));
            productService.GetActiveVendorsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vendor>>([vendor]));
            taxGroupService.GetActiveGroupsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>([taxGroup]));
            taxGroupService.GetActiveHSNCodesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<HSNCode>>([hsnCode]));
            taxGroupService.GetMappingByProductAsync(firstProduct.Id, Arg.Any<CancellationToken>())
                .Returns(new ProductTaxMapping
                {
                    ProductId = firstProduct.Id,
                    TaxGroupId = taxGroup.Id,
                    HSNCodeId = hsnCode.Id,
                    OverrideAllowed = true
                });
            taxGroupService.GetMappingByProductAsync(secondProduct.Id, Arg.Any<CancellationToken>())
                .Returns((ProductTaxMapping?)null);

            var viewModel = new ProductManagementViewModel(
                productService,
                taxGroupService,
                Substitute.For<INavigationService>(),
                regional,
                new ProductContextHolder());

            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            viewModel.SearchText = "Oxford";
            viewModel.FilterCategory = viewModel.Categories.FirstOrDefault();
            viewModel.FilterBrand = viewModel.Brands.FirstOrDefault();
            viewModel.FilterStockStatus = "In Stock";
            viewModel.SearchCommand.Execute(null);
            viewModel.SelectedProduct = firstProduct;
            viewModel.LoadMappingForProductCommand.ExecuteAsync(firstProduct.Id).GetAwaiter().GetResult();

            var view = new ProductManagementView
            {
                DataContext = viewModel,
                Width = 1440,
                Height = 980
            };

            ViewSnapshotVerifier.AssertMatches("ProductManagementView", view, 1440, 980);
        });
    }

    [Fact]
    public void PaymentManagementView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var paymentService = Substitute.For<IPaymentService>();
            var regional = Substitute.For<IRegionalSettingsService>();
            regional.CurrencySymbol.Returns("Rs.");

            var rahul = new Customer { Id = 1, Name = "Rahul Textiles" };
            var meera = new Customer { Id = 2, Name = "Meera Boutique" };
            var payments = new[]
            {
                new Payment
                {
                    Id = 101,
                    CustomerId = rahul.Id,
                    Customer = rahul,
                    PaymentDate = new DateTime(2026, 3, 24),
                    Amount = 2500,
                    Note = "Advance for tailoring order"
                },
                new Payment
                {
                    Id = 102,
                    CustomerId = meera.Id,
                    Customer = meera,
                    PaymentDate = new DateTime(2026, 3, 22),
                    Amount = 4200,
                    Note = "Settlement against March invoice"
                }
            };

            paymentService.GetCustomersAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Customer>>([rahul, meera]));
            paymentService.GetStatsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new PaymentStats(18, 154000)));
            paymentService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Payment>>(payments));

            var viewModel = new PaymentManagementViewModel(paymentService, regional);
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            viewModel.SelectedCustomer = rahul;
            viewModel.AmountText = "2500";
            viewModel.PaymentDate = new DateTime(2026, 3, 24);
            viewModel.Note = "Advance for order #1042";
            viewModel.SearchText = "Rahul";
            viewModel.ActiveDateFilter = "Month";
            viewModel.SearchCommand.Execute(null);

            var view = new PaymentManagementView
            {
                DataContext = viewModel,
                Width = 1320,
                Height = 920
            };

            ViewSnapshotVerifier.AssertMatches("PaymentManagementView", view, 1320, 920);
        });
    }

    [Fact]
    public void ReportsView_Should_Match_Approved_Snapshot_Fingerprint()
    {
        WpfTestApplication.Run(() =>
        {
            var reportsService = Substitute.For<IReportsService>();
            var printReportService = Substitute.For<IPrintReportService>();
            var printPreviewService = Substitute.For<IPrintPreviewService>();
            var auditService = Substitute.For<IAuditService>();

            reportsService.GetDailySalesSummaryAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(new DailySalesSummary((DateTime)call[0], 42, 185000, 5000, 180000, 27000, 3500)));
            reportsService.GetGrossProfitReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(new GrossProfitReport((DateTime)call[0], (DateTime)call[1], 420000, 265000, 155000, 36.9m, 128, 462)));
            reportsService.GetNetProfitReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(new NetProfitReport((DateTime)call[0], (DateTime)call[1], 420000, 265000, 155000, 32000, 123000, 29.3m)));
            reportsService.GetTaxReportAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TaxReport(2026, 3, 27000, 13500, 13500, 0,
                [
                    new HsnTaxSummaryLine("6205", 120000, 3000, 3000, 0, 6000, 2.5m, 2.5m)
                ])));
            reportsService.GetBestSellingProductsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ProductSalesSummary>>([
                    new ProductSalesSummary(10, "Oxford Shirt", "6205", 54, 80946, 3855, 2100, 1499),
                    new ProductSalesSummary(11, "Pleated Trouser", "6203", 21, 39879, 1900, 950, 1899)
                ]));
            reportsService.GetSlowMovingProductsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ProductSalesSummary>>([
                    new ProductSalesSummary(12, "Linen Waistcoat", "6211", 3, 8997, 428, 150, 2999)
                ]));
            reportsService.GetSalesByPaymentMethodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<PaymentMethodSummary>>([
                    new PaymentMethodSummary("UPI", 32, 142000, 68),
                    new PaymentMethodSummary("Cash", 10, 43000, 21),
                    new PaymentMethodSummary("Card", 6, 23000, 11)
                ]));
            reportsService.GetSalesByUserReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<UserSalesSummary>>([
                    new UserSalesSummary("Cashier", 61, 248000, 4100, 4065),
                    new UserSalesSummary("Manager", 12, 54000, 700, 4500)
                ]));
            reportsService.GetExpenseReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ExpenseReport(
                    9,
                    24500,
                    [new CategoryBreakdown("Rent", 12000), new CategoryBreakdown("Logistics", 6500), new CategoryBreakdown("Utilities", 6000)],
                    [new MonthlyTotal("Mar", 24500)],
                    [
                        new Expense { Date = new DateTime(2026, 3, 20), Category = "Rent", Amount = 12000 },
                        new Expense { Date = new DateTime(2026, 3, 22), Category = "Utilities", Amount = 6000 }
                    ])));
            reportsService.GetIroningReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new IroningReport(
                    24,
                    9600,
                    7200,
                    2400,
                    [
                        new IroningEntry { Date = new DateTime(2026, 3, 24), CustomerName = "Aditi", Amount = 450, IsPaid = true },
                        new IroningEntry { Date = new DateTime(2026, 3, 23), CustomerName = "Naren", Amount = 320, IsPaid = false }
                    ])));
            reportsService.GetOrderReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new OrderReport(
                    18,
                    68000,
                    11,
                    7,
                    [
                        new Order { Date = new DateTime(2026, 3, 24), CustomerName = "Rahul", Amount = 3500, Status = "Pending" },
                        new Order { Date = new DateTime(2026, 3, 23), CustomerName = "Meera", Amount = 4200, Status = "Delivered" }
                    ])));
            reportsService.GetInwardReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new InwardReport(
                    7,
                    18400,
                    [
                        new InwardEntry { InwardDate = new DateTime(2026, 3, 22), Vendor = new Vendor { Name = "Textiles Co" }, TransportCharges = 2200 },
                        new InwardEntry { InwardDate = new DateTime(2026, 3, 21), Vendor = new Vendor { Name = "Fabric House" }, TransportCharges = 1800 }
                    ])));
            reportsService.GetDebtorReportAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new DebtorReport(6, 42000, [new TopDebtor("Rahul Traders", 18000), new TopDebtor("Meera Boutique", 9500)])));

            var eventBus3 = Substitute.For<IEventBus>();
            var viewModel = new ReportsViewModel(reportsService, printReportService, printPreviewService, auditService, eventBus3);
            viewModel.LoadCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            viewModel.ActivePreset = "This Quarter";
            viewModel.DateFrom = new DateTime(2026, 1, 1);
            viewModel.DateTo = new DateTime(2026, 3, 25);

            var view = new ReportsView
            {
                DataContext = viewModel,
                Width = 1440,
                Height = 1080
            };

            ViewSnapshotVerifier.AssertMatches("ReportsView", view, 1440, 1080);
        });
    }
}
