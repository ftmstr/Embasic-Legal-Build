using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class DateEdgeCaseTests
{
    [Fact]
    public void ZeroOffsetPreservesDate()
    {
        var date = new DateOnly(2026, 11, 19);
        Assert.Equal(date, DateEngine.Shift(date, 0, 0, 0));
        var diff = DateEngine.Between(date, date);
        Assert.Equal((0, 0, 0, 0, false),
            (diff.Years, diff.Months, diff.Days, diff.TotalDays, diff.Reversed));
    }

    [Fact]
    public void LeapDayAndMonthEndHaveDeterministicClamp()
    {
        Assert.Equal(new DateOnly(2024, 2, 29),
            DateEngine.Shift(new DateOnly(2024, 3, 31), 0, -1, 0));
        Assert.Equal(new DateOnly(2025, 2, 28),
            DateEngine.Shift(new DateOnly(2024, 2, 29), 1, 0, 0));
    }

    [Fact]
    public void CrossYearAndReverseOrderAreCorrect()
    {
        var result = DateEngine.Between(new DateOnly(2027, 1, 1), new DateOnly(2026, 12, 31));
        Assert.Equal(1, result.TotalDays);
        Assert.True(result.Reversed);
        Assert.Equal(new DateOnly(2027, 1, 1),
            DateEngine.Shift(new DateOnly(2026, 12, 31), 0, 0, 1));
    }

    [Fact]
    public void LeapYearDifferenceHonorsClampedAnniversary()
    {
        var span = DateEngine.Between(new DateOnly(2024, 2, 29), new DateOnly(2025, 3, 1));
        Assert.Equal((1, 0, 1), (span.Years, span.Months, span.Days));
    }
}
