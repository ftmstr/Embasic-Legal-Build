using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur007PenaltyMoneyTests
{
    [Theory]
    [InlineData(100, 1, 6, true, 83)]
    [InlineData(100, 1, 6, false, 116)]
    [InlineData(120, 1, 2, true, 60)]
    [InlineData(120, 1, 2, false, 180)]
    public void FinalWholeDayFractionIsTruncatedOnlyAfterCalculation(
        int days, int n, int d, bool reduce, int expected)
    {
        var calc = PenaltyMoneyArithmetic.ApplyFraction(days, n, d, reduce);
        Assert.Equal(expected, calc.ResultDays);
        Assert.False(calc.JudicialSentenceValidated);
        Assert.False(calc.FavorableLawVerified);
    }

    [Fact]
    public void BadAndOverlongPenaltyFractionsAreRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => PenaltyMoneyArithmetic.ApplyFraction(0, 1, 6, true));
        Assert.ThrowsAny<ArgumentException>(() => PenaltyMoneyArithmetic.ApplyFraction(20, 2, 1, true));
        Assert.ThrowsAny<ArgumentException>(() => PenaltyMoneyArithmetic.ApplyFraction(1_000_000, 1, 1, false));
    }

    [Fact]
    public void FineMultiplicationKeepsAdditionalComponentSeparateFromWholeLira()
    {
        var value = PenaltyMoneyArithmetic.MultiplyDays(83, 100.25m, 50.75m);
        Assert.Equal(8320.75m, value.Multiplication);
        Assert.Equal(8320m, value.WholeLiraComponent);
        Assert.Equal(8370.75m, value.IllustrativeTotal);
        Assert.False(value.EnforcementAuthorized);
        Assert.ThrowsAny<ArgumentException>(() => PenaltyMoneyArithmetic.MultiplyDays(0, 100));
    }

    [Fact]
    public void Official7499EffectiveDateIsAnAdvisoryNotLegalCertification()
    {
        var older = PenaltyMoneyArithmetic.HistoricalRate(new DateOnly(2024, 5, 31));
        var newer = PenaltyMoneyArithmetic.HistoricalRate(new DateOnly(2024, 6, 1));
        Assert.Equal(20m, older.HistoricalMinimum);
        Assert.Equal(100m, newer.HistoricalMinimum);
        Assert.Equal(500m, newer.HistoricalMaximum);
        Assert.False(newer.FavorableLawReviewed);
        Assert.False(newer.DateSpecificApplicationCertified);
        Assert.Null(PenaltyMoneyArithmetic.HistoricalRate(new DateOnly(2004, 1, 1)).HistoricalMinimum);
    }

    [Fact]
    public void YdoIsManualCompoundingNotAnAutomaticLegalFineAdjustment()
    {
        var rates = PenaltyMoneyArithmetic.ParseManualRates("43,93; 25,49");
        Assert.Equal(2, rates.Count);
        var result = PenaltyMoneyArithmetic.CompoundRevaluation(1000m, rates);
        Assert.Equal(1000m * 1.4393m * 1.2549m, result);
        Assert.Throws<FormatException>(() => PenaltyMoneyArithmetic.ParseManualRates("43,93;"));
        Assert.Equal(1m, PenaltyMoneyArithmetic.OldLiraToLira(1_000_000m));
    }
}
