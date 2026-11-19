using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;
public sealed class Tur005ProceduralSystematicsTests
{
    [Fact]
    public void ThreeScreensContainCoreLawHooksAndSeparateSearchBranches()
    {
        Assert.Equal(3, ProceduralWorkflows.All.Count);
        Assert.All(ProceduralWorkflows.All, workflow =>
        {
            Assert.True(workflow.Checks.Count >= 10);
            Assert.Equal(workflow.Checks.Count,
                workflow.Checks.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.All(workflow.Checks, c => Assert.False(string.IsNullOrWhiteSpace(c.LegalReferences)));
        });
        var search = ProceduralWorkflows.SearchSeizure;
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("Yönetmelik m.8"));
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("CMK m.118"));
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("CMK m.119/4"));
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("CMK m.127/3"));
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("CMK m.134"));
        Assert.Contains(search.Checks, q => q.LegalReferences.Contains("Yönetmelik m.17"));
        Assert.Contains(search.Checks, q => q.Question.Contains("Danıştay"));
    }
    [Fact]
    public void AllUncheckedNeverReceivesApprovalOrLegalPass()
    {
        var w = ProceduralWorkflows.SearchSeizure;
        var result = ProceduralReviewEngine.Evaluate(w, []);
        Assert.Equal(w.Checks.Count, result.NotReviewed);
        Assert.False(result.ReviewRowsComplete);
        Assert.False(result.LegalComplianceCertified);
        Assert.False(result.SourceCorpusVerified);
    }
    [Fact]
    public void AllDocumentedEvenWithEvidenceReferencesIsNotCertified()
    {
        var w = ProceduralWorkflows.Detention;
        var result = ProceduralReviewEngine.Evaluate(w,
            w.Checks.Select(c => new ProceduralEvidence(c.Id, ProceduralEvidenceState.Documented,
                "SENTETİK belge referansı")));
        Assert.True(result.ReviewRowsComplete);
        Assert.Equal(w.Checks.Count,result.Documented);
        Assert.False(result.LegalComplianceCertified);
        Assert.False(result.JudgmentIssued);
    }
    [Fact]
    public void UnsupportedNotApplicableAndMissingEvidenceAreVisible()
    {
        var w = ProceduralWorkflows.JudicialControl;
        var result = ProceduralReviewEngine.Evaluate(w,
        [
            new ProceduralEvidence(w.Checks[0].Id,ProceduralEvidenceState.Documented,""),
            new ProceduralEvidence(w.Checks[1].Id,ProceduralEvidenceState.NotApplicable,""),
            new ProceduralEvidence(w.Checks[2].Id,ProceduralEvidenceState.Deficiency,"Eksik")
        ]);
        Assert.Equal(2,result.UnsupportedDeclarations);
        Assert.Equal(1,result.Deficiencies);
        Assert.False(result.ReviewRowsComplete);
    }
    [Fact]
    public void DuplicateOrInventedChecksAreRejected()
    {
        var w = ProceduralWorkflows.SearchSeizure;
        Assert.Throws<InvalidDataException>(()=>ProceduralReviewEngine.Evaluate(w,
        [
            new ProceduralEvidence("S01",ProceduralEvidenceState.Documented,"X"),
            new ProceduralEvidence("S01",ProceduralEvidenceState.Documented,"Y")
        ]));
        Assert.Throws<InvalidDataException>(()=>ProceduralReviewEngine.Evaluate(w,
        [new ProceduralEvidence("UNKNOWN",ProceduralEvidenceState.Documented,"X")]));
    }
}
