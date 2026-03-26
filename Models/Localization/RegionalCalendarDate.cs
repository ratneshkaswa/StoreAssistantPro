namespace StoreAssistantPro.Models.Localization;

/// <summary>
/// Represents a regional calendar system for display purposes.
/// Supports Vikram Samvat and Saka calendars alongside Gregorian.
/// </summary>
public enum RegionalCalendarType
{
    Gregorian,
    VikramSamvat,
    Saka
}

/// <summary>
/// Holds a converted regional calendar date for display.
/// </summary>
public sealed record RegionalCalendarDate(
    RegionalCalendarType CalendarType,
    int Year,
    int Month,
    int Day,
    string MonthName,
    string FormattedDate);
