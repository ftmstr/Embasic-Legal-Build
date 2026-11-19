using Embasic.Legal.Core;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur004SourceCoverageTests
{
    [Fact]
    public void MissingUnknownAndDuplicatePageMappingAreExplicit()
    {
        var list = new[] { "1", "2", "3" };
        var first = ArticleCoverageLedger.Fragment(1, "1", "SENTETİK madde bir");
        var second = ArticleCoverageLedger.Fragment(3, "5", "SENTETİK beklenmeyen");
        var review = ArticleCoverageLedger.Audit(list, new[] { first, first, second });
        Assert.False(review.StructuralConsistencyOnly);
        Assert.Contains("2", review.MissingArticles);
        Assert.Contains("3", review.MissingArticles);
        Assert.Contains("5", review.UnlistedArticles);
        Assert.Contains("1:1", review.DuplicatePageEntries);
        Assert.False(review.NoteBindingAuthorized);
        Assert.False(review.OfficialFullTextVerified);
    }

    [Fact]
    public void SyntheticCompleteLedgerNeverBecomesOfficialFullTextPass()
    {
        var rows = new[] {
            ArticleCoverageLedger.Fragment(1, "1", "SYNTHETIC A"),
            ArticleCoverageLedger.Fragment(2, "1", "SYNTHETIC CONTINUED"),
            ArticleCoverageLedger.Fragment(2, "2", "SYNTHETIC B")
        };
        var result = ArticleCoverageLedger.Audit(new[] { "1", "2" }, rows);
        Assert.True(result.StructuralConsistencyOnly);
        Assert.False(result.OfficialFullTextVerified);
        Assert.False(result.HistoricalEffectiveDateVerified);
        Assert.False(result.NoteBindingAuthorized);
    }

    [Fact]
    public void TamperedCandidateReceiptIsRejectedOnReadback()
    {
        string root = Path.Combine(Path.GetTempPath(), "embasic-cover-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            string pdf = Path.Combine(root, "synthetic.pdf");
            File.WriteAllText(pdf, "%PDF-1.4\nsynthetic mock (not a real law)");
            var store = new LocalPdfEvidenceStore(Path.Combine(root, "candidates"));
            var receipt = store.Import(pdf, "5237");
            Assert.Equal(PdfEvidenceStatus.ByteIntegrityOnly, store.Recheck(receipt).Status);
            File.AppendAllText(receipt.CandidateFile, "tampered");
            Assert.Throws<InvalidDataException>(() => store.Recheck(receipt));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
