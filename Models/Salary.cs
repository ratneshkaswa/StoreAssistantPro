using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace StoreAssistantPro.Models;

public class Salary
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Month { get; set; } = string.Empty;

    public int Year { get; set; }

    public decimal Amount { get; set; }

    public decimal BaseSalary { get; set; }

    public decimal Advance { get; set; }

    public int PresentDays { get; set; }

    public int AbsentDays { get; set; }

    public decimal HoursWorked { get; set; }

    public decimal Incentive { get; set; }

    public DateTime? PaidDate { get; set; }

    public bool IsPaid { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // ── Computed Properties ──

    [NotMapped]
    public int DaysInMonth
    {
        get
        {
            if (MonthIndex > 0 && Year > 0)
                return DateTime.DaysInMonth(Year, MonthIndex);
            return 30;
        }
    }

    [NotMapped]
    public decimal OvertimeHours => HoursWorked > 0 ? HoursWorked : 0;

    [NotMapped]
    public int PenaltyCount => AbsentDays >= 12 ? 3 : AbsentDays >= 8 ? 2 : AbsentDays >= 4 ? 1 : 0;

    [NotMapped]
    public decimal PenaltyAmount => PenaltyCount > 0
        ? PenaltyCount * -RoundUp(BaseSalary / 30m)
        : 0;

    [NotMapped]
    public string PenaltyDisplay => PenaltyCount > 0
        ? $"({PenaltyCount}) {PenaltyAmount:N0}"
        : "";

    [NotMapped]
    public decimal MonthAdjustment => DaysInMonth != 30 && BaseSalary > 0
        ? RoundUp((BaseSalary / 30m) * (DaysInMonth - 30))
        : 0;

    [NotMapped]
    public decimal NetPay
    {
        get
        {
            if (BaseSalary <= 0) return Amount;
            var raw = BaseSalary
                      + (PenaltyCount * -(BaseSalary / 30m))
                      + (AbsentDays * -(BaseSalary / 30m))
                      + (OvertimeHours * (BaseSalary / 270m))
                      + Incentive
                      + ((BaseSalary / 30m) * (DaysInMonth - 30));
            return RoundUp(raw) - Advance;
        }
    }

    [NotMapped]
    public int MonthIndex
    {
        get
        {
            for (int i = 1; i <= 12; i++)
                if (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                    .Equals(Month, StringComparison.OrdinalIgnoreCase))
                    return i;
            return 0;
        }
    }

    /// <summary>Count of Tuesdays (weekly holiday) in the salary month.</summary>
    [NotMapped]
    public int TuesdaysInMonth
    {
        get
        {
            if (MonthIndex <= 0 || Year <= 0) return 4;
            int count = 0;
            int days = DaysInMonth;
            for (int d = 1; d <= days; d++)
            {
                if (new DateTime(Year, MonthIndex, d).DayOfWeek == DayOfWeek.Tuesday)
                    count++;
            }
            return count;
        }
    }

    /// <summary>Working days = DaysInMonth minus Tuesdays (holiday).</summary>
    [NotMapped]
    public int WorkingDays => DaysInMonth - TuesdaysInMonth;

    public static decimal RoundUp(decimal value)
        => value >= 0 ? Math.Ceiling(value) : -Math.Ceiling(-value);
}
