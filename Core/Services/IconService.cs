namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Central glyph lookup for shell navigation and quick actions.
/// Keeps icon choices consistent anywhere the shell renders a page/action badge.
/// </summary>
public sealed class IconService
{
    private static readonly IReadOnlyDictionary<string, string> Glyphs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home"] = "\uE80F",
            ["Firm"] = "\uE821",
            ["Users"] = "\uE716",
            ["Tax"] = "\uE8C8",
            ["Vendors"] = "\uE8D4",
            ["Products"] = "\uE719",
            ["Categories"] = "\uE8EC",
            ["Brands"] = "\uE8D2",
            ["Inward"] = "\uE8B7",
            ["Inventory"] = "\uE8B8",
            ["Billing"] = "\uE8C7",
            ["SaleHistory"] = "\uE81C",
            ["Customers"] = "\uE77B",
            ["PurchaseOrders"] = "\uE8A1",
            ["FinancialYear"] = "\uE787",
            ["Settings"] = "\uE713",
            ["Expenses"] = "\uEAFD",
            ["Debtors"] = "\uE8C7",
            ["Orders"] = "\uE7BF",
            ["Ironing"] = "\uE8A5",
            ["Salaries"] = "\uE9D2",
            ["Branches"] = "\uE7F1",
            ["SalesPurchase"] = "\uE8A1",
            ["Payments"] = "\uE8C7",
            ["Reports"] = "\uE9D2",
            ["BarcodeLabels"] = "\uE8A3",
            ["Refresh"] = "\uE72C",
            ["Shortcuts"] = "\uE765",
            ["CommandPalette"] = "\uEA37",
            ["Search"] = "\uE721",
            ["Logout"] = "\uE8AC"
        };

    public string GetGlyph(string key) =>
        Glyphs.TryGetValue(key, out var glyph)
            ? glyph
            : "\uE10F";
}
