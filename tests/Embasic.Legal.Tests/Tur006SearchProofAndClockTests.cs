using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur006SearchProofAndClockTests
{
    [Fact]
    public void SourceDisputesNeverTurnDateIntoAnOfficialLegalPass()
    {
        Assert.Contains(AdliSearchNormGate.Risks, r => r.Id == "R08A" && r.AffectedCheck == "S09");
        Assert.Contains(AdliSearchNormGate.Risks, r => r.Id == "R08F");
        Assert.Contains(AdliSearchNormGate.Risks, r => r.Id == "R30");
        Assert.Contains(AdliSearchNormGate.Risks, r => r.Id == "R17" && r.AffectedCheck == "S24");
        var before = AdliSearchNormGate.Assess(new DateOnly(2005, 6, 2));
        var after = AdliSearchNormGate.Assess(new DateOnly(2026, 11, 20));
        Assert.False(before.HistoricalApplicabilityVerified);
        Assert.False(after.HistoricalApplicabilityVerified);
        Assert.False(after.AuthorizesLegalCompliancePass);
        Assert.Equal(AdliSearchNormGate.Risks.Count, after.UnresolvedRisks);
        Assert.All(AdliSearchNormGate.Risks, r => Assert.False(r.CertifiedPrimaryDecision));
    }

    [Fact]
    public void ClockReferencesAre24HoursFromSubmissionAnd48HoursFromSeizure()
    {
        var seized = new DateTimeOffset(2026, 11, 20, 1, 0, 0, TimeSpan.FromHours(3));
        var within = SeizureClockAudit.Compare(seized, seized.AddHours(24), seized.AddHours(48));
        Assert.Equal(MechanicalDeadlineResult.WithinReferenceWindow, within.SubmittedWithin24h);
        Assert.Equal(MechanicalDeadlineResult.WithinReferenceWindow, within.JudgeDecisionWithin48h);
        Assert.False(within.LegalDeadlineCertified);
        Assert.False(within.AutomaticSeizureReleaseDecision);
        var late = SeizureClockAudit.Compare(seized, seized.AddHours(24).AddTicks(1),
            seized.AddHours(48).AddTicks(1));
        Assert.Equal(MechanicalDeadlineResult.ExceedsReferenceWindow, late.SubmittedWithin24h);
        Assert.Equal(MechanicalDeadlineResult.ExceedsReferenceWindow, late.JudgeDecisionWithin48h);
    }

    [Fact]
    public void OffsetConversionUsesInstantsAndMissingDatesFailClosed()
    {
        var start = new DateTimeOffset(2026, 10, 25, 1, 15, 0, TimeSpan.FromHours(3));
        var submitted = start.AddHours(23).ToOffset(TimeSpan.FromHours(2));
        var decided = start.AddHours(47).ToOffset(TimeSpan.FromHours(2));
        var flags = SeizureClockAudit.Compare(start, submitted, decided);
        Assert.Equal(MechanicalDeadlineResult.WithinReferenceWindow, flags.SubmittedWithin24h);
        Assert.Equal(MechanicalDeadlineResult.WithinReferenceWindow, flags.JudgeDecisionWithin48h);
        Assert.Equal(MechanicalDeadlineResult.MissingEvidence,
            SeizureClockAudit.Compare(null, submitted, decided).SubmittedWithin24h);
        Assert.Equal(MechanicalDeadlineResult.MissingEvidence,
            SeizureClockAudit.Compare(start, null, null).JudgeDecisionWithin48h);
    }

    [Fact]
    public void ReversedTimelineCannotProduceAReferenceWindowPass()
    {
        var start = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.FromHours(3));
        var r = SeizureClockAudit.Compare(start, start.AddHours(3), start.AddHours(2));
        Assert.Equal(MechanicalDeadlineResult.ChronologyConflict, r.SubmittedWithin24h);
        Assert.Equal(MechanicalDeadlineResult.ChronologyConflict, r.JudgeDecisionWithin48h);
    }

    [Fact]
    public void BareClockWithoutExplicitOffsetIsNotTrusted()
    {
        Assert.False(SeizureClockAudit.TryParseLocalWithOffset("2026-11-20 12:30", out _));
        Assert.False(SeizureClockAudit.TryParseLocalWithOffset("20.11.2026 12:30", out _));
        Assert.True(SeizureClockAudit.TryParseLocalWithOffset("2026-11-20T12:30+03:00", out _));
    }
}
