using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur008CourtAndSeizureGateTests
{
    [Fact]
    public void OfficialCourtSearchLandingIsNotAProofOfOriginalDecision()
    {
        Assert.StartsWith("https://karararama.danistay.gov.tr/",
            CourtDecisionEvidenceRegister.OfficialSearchPortalUrl);
        Assert.False(CourtDecisionEvidenceRegister.AllOfficialOriginalsRetrievedAndVerified);
        Assert.Contains("henüz edinilmedi", CourtDecisionEvidenceRegister.OfficialOriginalStatus);
        Assert.All(CourtDecisionEvidenceRegister.Entries,
            x => Assert.False(x.OfficialDecisionOriginalVerified));
    }

    [Fact]
    public void CompleteDocumentReferencesNeverHideMechanicallyLateSubmission()
    {
        var t = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(3));
        var r = SeizureDocumentGate.Examine(new(
            t, "SYNTHETIC_SEIZURE", t.AddHours(25), "SYNTHETIC_SUBMISSION",
            t.AddHours(47), "SYNTHETIC_JUDGE"));
        Assert.True(r.DocumentMetadataComplete); // references exist; NOT a legal pass
        Assert.True(r.MechanicalTimeAlert);
        Assert.Contains("aşım riski", r.Warning);
        Assert.False(r.StatutoryComplianceVerified);
        Assert.False(r.EvidenceAuthenticityVerified);
    }

    [Fact]
    public void BothWindowsWithinReferenceStillNeverAuthenticateOriginalDocuments()
    {
        var t = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(3));
        var r = SeizureDocumentGate.Examine(new(
            t, "SYNTHETIC_SEIZURE", t.AddHours(23), "SYNTHETIC_SUBMISSION",
            t.AddHours(47), "SYNTHETIC_JUDGE"));
        Assert.False(r.MechanicalTimeAlert);
        Assert.False(r.StatutoryComplianceVerified);
        Assert.Contains("kanıtı değildir", r.Warning);
    }
}
