using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur015Cmk7571SpecialNormTests
{
    [Theory]
    [InlineData("142/2-e")]
    [InlineData("158/1-f")]
    [InlineData("158/1-l")]
    [InlineData("245")]
    public void ListedOffenseIsOnlyAnEvidenceReviewSignal(string offense)
    {
        var review = Cmk7571SpecialNormGate.ReviewCmk128A(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), offense);
        Assert.True(review.OffenseInEnumeratedList);
        Assert.True(review.RequiresSpecialDocumentReview);
        Assert.False(review.AutomaticSeizureOrSuspensionAuthorization);
        Assert.False(review.GenericCmk127DeadlineCanCertifyThisMeasure);
    }

    [Fact]
    public void Cmk128ARejectsBroad158ShortcutAndMissingActDate()
    {
        var review = Cmk7571SpecialNormGate.ReviewCmk128A(
            new DateOnly(2026, 1, 1), null, "158");
        Assert.False(review.OffenseInEnumeratedList);
        Assert.Equal("MEASURE_DATE_MISSING", review.MeasureDatePosition);
        Assert.False(review.OfficialCaseSpecificCorpusVerified);
    }

    [Fact]
    public void PreEnactmentIncidentDoesNotEraseLaterProceduralActDate()
    {
        var review = Cmk7571SpecialNormGate.ReviewCmk128A(
            new DateOnly(2025, 10, 1), new DateOnly(2026, 1, 1), "245");
        Assert.True(review.IncidentPredatesMeasureLaw);
        Assert.Equal("ON_OR_AFTER_2025_12_25", review.MeasureDatePosition);
        Assert.False(review.AutomaticSeizureOrSuspensionAuthorization);
    }

    [Fact]
    public void HistoricalSettlementWithPendingCaseIsOnlyCandidateNotApproval()
    {
        var review = Cmk7571SpecialNormGate.ReviewCmkTemporary8(
            new DateOnly(2025, 12, 24), true);
        Assert.Equal(true, review.HistoricalTransitionCandidate);
        Assert.False(review.AutomaticTransitionOrMediationOutcome);
    }

    [Fact]
    public void PublicationDayOrMissingCaseEvidenceBlocksTransitionConclusion()
    {
        var onDay = Cmk7571SpecialNormGate.ReviewCmkTemporary8(
            new DateOnly(2025, 12, 25), true);
        var missing = Cmk7571SpecialNormGate.ReviewCmkTemporary8(
            new DateOnly(2025, 12, 24), null);
        var after = Cmk7571SpecialNormGate.ReviewCmkTemporary8(
            new DateOnly(2026, 1, 5), true);
        Assert.Null(onDay.HistoricalTransitionCandidate);
        Assert.Null(missing.HistoricalTransitionCandidate);
        Assert.Equal(false, after.HistoricalTransitionCandidate);
        Assert.False(after.AutomaticTransitionOrMediationOutcome);
    }
}
