using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur007EvidenceSourceAndDocumentsTests
{
    [Fact]
    public void CourtDecision2007PublicationReproducesAnnulledAndRejectedSeparateProvisions()
    {
        var d = CourtDecisionEvidenceRegister.Entries.Single(x => x.Key == "D2007");
        Assert.Equal(CourtEvidenceLevel.PublishedDecisionReproduction, d.Level);
        Assert.Contains("m.8/1(c)", d.RecordedDisposition);
        Assert.Contains("m.27", d.RecordedDisposition);
        Assert.Contains("reddi", d.RecordedDisposition);
        Assert.Contains("30/1", d.RecordedDisposition);
        Assert.False(d.OfficialDecisionOriginalVerified);
        Assert.All(CourtDecisionEvidenceRegister.Entries, x =>
            Assert.False(x.EffectiveOnCaseDateVerified));
    }

    [Fact]
    public void NewerYDAnd2012OnamaRemainSecondaryOnly()
    {
        Assert.Equal(CourtEvidenceLevel.SecondarySummary,
            CourtDecisionEvidenceRegister.Entries.Single(x=>x.Key=="D2012").Level);
        Assert.Equal(CourtEvidenceLevel.SecondarySummary,
            CourtDecisionEvidenceRegister.Entries.Single(x=>x.Key=="D2025").Level);
        var review = CourtDecisionEvidenceRegister.Assess(new DateOnly(2026, 11, 20));
        Assert.Equal(1, review.PublishedDecisionReproductions);
        Assert.Equal(2, review.SecondaryOnlyDecisions);
        Assert.False(review.NormTimelineComplete);
        Assert.False(review.LegalEffectCertified);
        Assert.False(review.EnablesSearchOrSeizure);
    }

    [Fact]
    public void TimelineRequiresReferencesAndNeverVerifiesTheirAuthenticity()
    {
        var t = new DateTimeOffset(2026, 11, 20, 12, 30, 0, TimeSpan.FromHours(3));
        var missing = SeizureDocumentGate.Examine(
            new(t, "", t.AddHours(12), "SENTETIK SUNUM", t.AddHours(23), ""));
        Assert.Equal(2, missing.MissingDocumentReferences);
        Assert.False(missing.DocumentMetadataComplete);
        Assert.False(missing.EvidenceAuthenticityVerified);
        var filled = SeizureDocumentGate.Examine(
            new(t, "SENTETIK ELKOYMA", t.AddHours(12), "SENTETIK SUNUM",
                t.AddHours(23), "SENTETIK KARAR"));
        Assert.True(filled.DocumentMetadataComplete);
        Assert.False(filled.StatutoryComplianceVerified);
        Assert.False(filled.EvidenceAuthenticityVerified);
    }

    [Fact]
    public void TimeOrChronologyConflictsNeverCountAsCompleteDocuments()
    {
        var t = new DateTimeOffset(2026, 11, 20, 12, 30, 0, TimeSpan.FromHours(3));
        var noTime = SeizureDocumentGate.Examine(
            new(t, "E1", null, "E2", null, "E3"));
        Assert.False(noTime.DocumentMetadataComplete);
        var reversed = SeizureDocumentGate.Examine(
            new(t, "E1", t.AddHours(14), "E2", t.AddHours(13), "E3"));
        Assert.False(reversed.DocumentMetadataComplete);
        Assert.Equal(MechanicalDeadlineResult.ChronologyConflict,
            reversed.Clock.SubmittedWithin24h);
    }
}
