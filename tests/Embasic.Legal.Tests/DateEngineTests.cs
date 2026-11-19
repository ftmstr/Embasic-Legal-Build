using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class DateEngineTests
{
    [Fact]
    public void ReferenceAndIncidentCalendarDifference()
    {
        var result = DateEngine.Between(new DateOnly(2024, 3, 15), new DateOnly(2026, 11, 19));
        Assert.Equal((2, 8, 4, 979, false),
            (result.Years, result.Months, result.Days, result.TotalDays, result.Reversed));
    }

    [Fact]
    public void ReversedInputsPreserveAbsoluteDuration()
    {
        var result = DateEngine.Between(new DateOnly(2026, 11, 19), new DateOnly(2024, 3, 15));
        Assert.True(result.Reversed);
        Assert.Equal(979, result.TotalDays);
    }

    [Fact]
    public void MonthEndClampsAccordingToDateOnly()
    {
        Assert.Equal(new DateOnly(2025, 2, 28),
            DateEngine.Shift(new DateOnly(2025, 1, 31), 0, 1, 0));
    }

    [Fact]
    public void LeapYearClampsWhenAddingYear()
    {
        Assert.Equal(new DateOnly(2025, 2, 28),
            DateEngine.Shift(new DateOnly(2024, 2, 29), 1, 0, 0));
    }

    [Fact]
    public void CompoundOffsetIsAppliedInYearsMonthsDaysOrder()
    {
        Assert.Equal(new DateOnly(2027, 9, 16),
            DateEngine.Shift(new DateOnly(2025, 6, 1), 2, 3, 15));
    }

    [Fact]
    public void OverflowMustFailClosed() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DateEngine.Shift(new DateOnly(9999, 12, 31), 0, 0, 1));

    [Fact]
    public void SourceDirectoryMustNotBeCalledVerified()
    {
        Assert.False(LegislationCatalog.OfficialFullTextVerified);
        Assert.Equal(15, LegislationCatalog.InitialDirectory.Count);
    }
}
