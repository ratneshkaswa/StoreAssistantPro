namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public sealed record RecentSaleDisplayItem(
    string InvoiceNumber,
    string RelativeDate,
    string DateToolTip,
    string Amount,
    string PaymentMethod,
    string ItemCountLabel);

public sealed record TopProductDisplayItem(
    string ProductName,
    string QuantitySoldLabel,
    string RevenueCompact,
    string RevenueToolTip);

/// <summary>Display item for daily sales trend bar (#398).</summary>
public sealed record DailySalesTrendDisplayItem(
    string DateLabel,
    string AmountFormatted,
    int TransactionCount,
    double BarWidthRatio);

/// <summary>Display item for payment method breakdown (#401).</summary>
public sealed record PaymentMethodDisplayItem(
    string Method,
    string AmountFormatted,
    decimal Percentage,
    int Count)
{
    public string PercentageLabel => $"{Percentage}%";
    public string CountLabel => Count == 1 ? "1 bill" : $"{Count} bills";
}

public enum KpiTrendTone
{
    Positive,
    Negative,
    Neutral
}

public sealed record KpiTrendDisplayItem(
    string Glyph,
    string Label,
    KpiTrendTone Tone);
