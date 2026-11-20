using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur008ArithmeticDraftSafetyTests
{
    [Fact]
    public void NoResultIsEverCurrentByDefault()
    {
        var state = new ArithmeticDraftGuard();
        Assert.False(state.HasCurrentResult);
        Assert.Equal(0, state.InputRevision);
    }

    [Fact]
    public void EditedInputImmediatelyRevokesPreviouslySuccessfulCalculation()
    {
        var state = new ArithmeticDraftGuard();
        state.CalculationSucceeded();
        Assert.True(state.HasCurrentResult);
        state.InputChanged();
        Assert.False(state.HasCurrentResult);
        Assert.Equal(1, state.InputRevision);
        state.CalculationSucceeded();
        Assert.True(state.HasCurrentResult);
        state.InputChanged();
        Assert.False(state.HasCurrentResult);
    }

    [Fact]
    public void HistoricalFineRateAndYdoCannotConferLegalAuthorization()
    {
        var fine = PenaltyMoneyArithmetic.MultiplyDays(90, 125m);
        Assert.False(fine.JudicialConversionAllowed);
        Assert.False(fine.EnforcementAuthorized);
        var rate = PenaltyMoneyArithmetic.HistoricalRate(new DateOnly(2024, 6, 1));
        Assert.False(rate.FavorableLawReviewed);
        Assert.False(rate.DateSpecificApplicationCertified);
        Assert.Equal(100m, rate.HistoricalMinimum);
        Assert.Equal(500m, rate.HistoricalMaximum);
    }
}
