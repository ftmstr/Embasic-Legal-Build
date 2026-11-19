namespace Embasic.Legal.Core;

/// <summary>Calendar arithmetic only. This module never computes statutory deadlines.</summary>
public static class DateEngine
{
    /// <summary>Apply years, then months, then days. DateOnly clamps month ends by .NET rules.</summary>
    public static DateOnly Shift(DateOnly date, int years, int months, int days) =>
        date.AddYears(years).AddMonths(months).AddDays(days);

    public static CalendarDifference Between(DateOnly first, DateOnly second)
    {
        bool reversed = first > second;
        DateOnly from = reversed ? second : first;
        DateOnly to = reversed ? first : second;
        int years = to.Year - from.Year;
        DateOnly atYear = from.AddYears(years);
        if (atYear > to)
        {
            years--;
            atYear = from.AddYears(years);
        }

        int months = (to.Year - atYear.Year) * 12 + to.Month - atYear.Month;
        DateOnly atMonth = atYear.AddMonths(months);
        if (atMonth > to)
        {
            months--;
            atMonth = atYear.AddMonths(months);
        }

        return new CalendarDifference(years, months, to.DayNumber - atMonth.DayNumber,
            to.DayNumber - from.DayNumber, reversed);
    }
}

public sealed record CalendarDifference(int Years, int Months, int Days, int TotalDays, bool Reversed);
