namespace StoreAssistantPro.Core.Features;

/// <summary>
/// Well-known feature names used across the application.
/// Centralised here so ViewModels and modules reference constants
/// instead of magic strings.
/// </summary>
public static class FeatureFlags
{
    public const string Products = "Products";
    public const string Sales = "Sales";
    public const string Billing = "Billing";
    public const string SystemSettings = "SystemSettings";
    public const string Reports = "Reports";
    public const string AdvancedBilling = "AdvancedBilling";
    public const string UserManagement = "UserManagement";
    public const string FirmManagement = "FirmManagement";
    public const string TaxManagement = "TaxManagement";
    public const string VendorManagement = "VendorManagement";
    public const string FinancialYear = "FinancialYear";
    public const string InwardEntry = "InwardEntry";
    public const string Inventory = "Inventory";
    public const string Customers = "Customers";
    public const string PurchaseOrders = "PurchaseOrders";
    public const string Expenses = "Expenses";
    public const string Debtors = "Debtors";
    public const string Orders = "Orders";
    public const string Ironing = "Ironing";
    public const string Salaries = "Salaries";
    public const string Branch = "Branch";
    public const string SalesPurchase = "SalesPurchase";
    public const string Payments = "Payments";
    public const string Categories = "Categories";
    public const string Brands = "Brands";
    public const string SaleHistory = "SaleHistory";
    public const string CashRegister = "CashRegister";
    public const string Backup = "Backup";
    public const string Quotations = "Quotations";
    public const string GRN = "GRN";
    public const string BarcodeLabels = "BarcodeLabels";

    // ── G1: Hardware Integration ──
    public const string BarcodeScanner = "BarcodeScanner";
    public const string ThermalPrinter = "ThermalPrinter";
    public const string CashDrawer = "CashDrawer";
    public const string WeighingScale = "WeighingScale";
    public const string LabelPrinter = "LabelPrinter";
    public const string CustomerDisplay = "CustomerDisplay";

    // ── G2: AI & Smart Features ──
    public const string SalesForecasting = "SalesForecasting";
    public const string SmartPricing = "SmartPricing";
    public const string CustomerInsights = "CustomerInsights";
    public const string SmartSearch = "SmartSearch";
    public const string AnomalyDetection = "AnomalyDetection";

    // ── G4: Localization (selected) ──
    public const string IndianNumberFormat = "IndianNumberFormat";
    public const string RegionalDateFormats = "RegionalDateFormats";
    public const string RegionalCalendar = "RegionalCalendar";
    public const string StateWiseTaxLabels = "StateWiseTaxLabels";
    public const string RegionalReceipts = "RegionalReceipts";

    // ── G7: API & Integration ──
    public const string RestApi = "RestApi";
    public const string ApiAuthentication = "ApiAuthentication";
    public const string ApiRateLimiting = "ApiRateLimiting";
    public const string AccountingExport = "AccountingExport";
    public const string CommunicationApi = "CommunicationApi";
    public const string WebhookOutbound = "WebhookOutbound";
    public const string NotificationTemplates = "NotificationTemplates";

    // ── G8: Mobile Companion ──
    public const string MobileCompanion = "MobileCompanion";
    public const string MobileOfflineSync = "MobileOfflineSync";

    // ── G13: HR / Staff ──
    public const string StaffAttendance = "StaffAttendance";
    public const string ShiftScheduling = "ShiftScheduling";
    public const string LeaveManagement = "LeaveManagement";
    public const string StaffPerformance = "StaffPerformance";
    public const string PayrollExport = "PayrollExport";

    // ── G15: UI Polish ──
    public const string PageAnimations = "PageAnimations";
    public const string SkeletonScreens = "SkeletonScreens";
    public const string ProgressIndicators = "ProgressIndicators";
    public const string StatusBadges = "StatusBadges";
    public const string ChartWidgets = "ChartWidgets";

    // ── G16: DB Administration ──
    public const string DatabaseMonitor = "DatabaseMonitor";
    public const string DatabaseMaintenance = "DatabaseMaintenance";
    public const string MigrationManager = "MigrationManager";
    public const string DataManagement = "DataManagement";
    public const string DataTransfer = "DataTransfer";

    // ── G17: Document Management ──
    public const string PdfGeneration = "PdfGeneration";
    public const string DocumentStorage = "DocumentStorage";
    public const string DocumentEmail = "DocumentEmail";
    public const string PrintQueue = "PrintQueue";
    public const string DocumentTemplates = "DocumentTemplates";

    // ── G18: Workflow Automation ──
    public const string WorkflowAutomation = "WorkflowAutomation";
    public const string AutomationRules = "AutomationRules";

    // ── G19: User Preferences ──
    public const string UserPreferences = "UserPreferences";
    public const string NotificationPreferences = "NotificationPreferences";
    public const string QuietHours = "QuietHours";

    // ── G3: Commercial ──
    public const string Licensing = "Licensing";
    public const string Subscription = "Subscription";
    public const string WhiteLabel = "WhiteLabel";
    public const string UsageAnalytics = "UsageAnalytics";

    // ── G5: Multi-Store ──
    public const string MultiStore = "MultiStore";
    public const string StockTransfer = "StockTransfer";
    public const string InterStoreSync = "InterStoreSync";

    // ── G6: E-commerce ──
    public const string EcommerceIntegration = "EcommerceIntegration";
    public const string ProductSync = "ProductSync";
    public const string OnlineOrders = "OnlineOrders";
    public const string MarketplaceCommission = "MarketplaceCommission";

    // ── G9: Multi-Country ──
    public const string MultiCurrency = "MultiCurrency";
    public const string CountryProfiles = "CountryProfiles";

    // ── G10: Niche Vertical ──
    public const string Alterations = "Alterations";
    public const string RentalManagement = "RentalManagement";
    public const string WholesalePricing = "WholesalePricing";
    public const string Consignment = "Consignment";
    public const string SeasonManagement = "SeasonManagement";
    public const string GiftCards = "GiftCards";
    public const string LoyaltyProgram = "LoyaltyProgram";

    // ── G11: Touch / Kiosk ──
    public const string TouchMode = "TouchMode";
    public const string GestureShortcuts = "GestureShortcuts";
    public const string OnScreenInput = "OnScreenInput";

    // ── G12: CRM ──
    public const string Campaigns = "Campaigns";
    public const string AutoGreeting = "AutoGreeting";
    public const string CustomerFeedback = "CustomerFeedback";
    public const string ServiceTickets = "ServiceTickets";
    public const string CrmTemplates = "CrmTemplates";

    // ── G14: Compliance ──
    public const string GstReturns = "GstReturns";
    public const string EWayBill = "EWayBill";
    public const string EInvoice = "EInvoice";
    public const string DataCompliance = "DataCompliance";

    // ── G20: Payment Gateway ──
    public const string PaymentGateway = "PaymentGateway";
    public const string PaymentReconciliation = "PaymentReconciliation";
    public const string PaymentSchedules = "PaymentSchedules";

    // ── G21: Budgeting ──
    public const string Budgeting = "Budgeting";
    public const string BudgetForecast = "BudgetForecast";
    public const string FinancialGoals = "FinancialGoals";

    // ── G22: Advanced Reporting ──
    public const string CustomReports = "CustomReports";
    public const string ReportScheduling = "ReportScheduling";
    public const string AdvancedAnalytics = "AdvancedAnalytics";
    public const string ReportAccess = "ReportAccess";
}
