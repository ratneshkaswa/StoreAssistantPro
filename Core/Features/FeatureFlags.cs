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
}
