using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur009SeizureAuthorizationTests
{
    private static SeizureDocumentInputs Example() {
        var seized = new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.FromHours(3));
        return new(seized, "SYNTHETIC_A", seized.AddHours(25), "SYNTHETIC_B",
            seized.AddHours(45), "SYNTHETIC_C");
    }

    [Fact]
    public void MissingPriorOrderEvidenceMustNotComputeThe24And48HourReference()
    {
        var r = SeizureAuthorizationGate.Assess(SeizureAuthorizationBasis.NotEstablished, Example());
        Assert.False(r.HasMechanicalTimeComparison);
        Assert.Null(r.TimeAudit);
        Assert.Equal(SeizureClockApplicability.RequiresBasisEvidence, r.Applicability);
        Assert.False(r.LegalConsequenceApproved);
    }

    [Fact]
    public void UserClaimOfPriorJudgeOrderDoesNotCreateAnOfficiallyVerifiedOrder()
    {
        var r = SeizureAuthorizationGate.Assess(
            SeizureAuthorizationBasis.PriorJudicialOrderDeclared, Example());
        Assert.False(r.HasMechanicalTimeComparison);
        Assert.Equal(SeizureClockApplicability.PriorOrderReportedNo127ThirdParagraphCheck,
            r.Applicability);
        Assert.False(r.BasisDocumentAuthenticityVerified);
        Assert.False(r.CourtDecisionCertified);
    }

    [Fact]
    public void JudgeLessSeizureGivesOnlyMechanicalAlertNotReleaseDecision()
    {
        var r = SeizureAuthorizationGate.Assess(
            SeizureAuthorizationBasis.WithoutPriorJudicialOrderDeclared, Example());
        Assert.True(r.HasMechanicalTimeComparison);
        Assert.True(r.TimeAudit!.MechanicalTimeAlert);
        Assert.Equal(MechanicalDeadlineResult.ExceedsReferenceWindow,
            r.TimeAudit.Clock.SubmittedWithin24h);
        Assert.False(r.TimeAudit.Clock.AutomaticSeizureReleaseDecision);
        Assert.False(r.LegalConsequenceApproved);
        Assert.False(r.NormVersionVerified);
    }

    [Fact]
    public void UnknownEnumValuesFailClosed()
    {
        var r = SeizureAuthorizationGate.Assess((SeizureAuthorizationBasis)999, Example());
        Assert.False(r.HasMechanicalTimeComparison);
        Assert.Equal(SeizureClockApplicability.RequiresBasisEvidence, r.Applicability);
    }

    [Fact]
    public void EvenExact24And48HoursWithReferencesDoesNotAuthorizeJudicialResult()
    {
        var seized = new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.FromHours(3));
        var refs = new SeizureDocumentInputs(seized, "SYNTHETIC_A",
            seized.AddHours(24), "SYNTHETIC_B", seized.AddHours(48), "SYNTHETIC_C");
        var r = SeizureAuthorizationGate.Assess(
            SeizureAuthorizationBasis.WithoutPriorJudicialOrderDeclared, refs);
        Assert.False(r.TimeAudit!.MechanicalTimeAlert);
        Assert.True(r.TimeAudit.DocumentMetadataComplete);
        Assert.False(r.LegalConsequenceApproved);
        Assert.False(r.CourtDecisionCertified);
    }
}
