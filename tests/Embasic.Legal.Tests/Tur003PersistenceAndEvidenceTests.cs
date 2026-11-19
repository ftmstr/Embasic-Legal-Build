using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur003PersistenceAndEvidenceTests
{
    private static string NewDirectory() =>
        Path.Combine(Path.GetTempPath(), "embasic-tur003-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void BackupIsAtomicAndPreservesSnapshotAfterSourceChanges()
    {
        string dir = NewDirectory();
        try
        {
            var key = new ArticleNoteKey("5237", "6", "1-e", "SYNTHETIC_SOURCE_A");
            string db = Path.Combine(dir, "notes.sqlite3");
            string snapshot = Path.Combine(dir, "snapshot.sqlite3");
            using (var store = new ArticleNoteStore(db))
            {
                store.Save(key, "İlk not, Türkçe");
                Assert.True(store.CheckIntegrity());
                Assert.Equal(Path.GetFullPath(snapshot), store.BackupTo(snapshot));
                store.Save(key, "İkinci not");
                Assert.Throws<ArgumentException>(() => store.BackupTo(db));
            }
            using var backup = new ArticleNoteStore(snapshot);
            Assert.Equal("İlk not, Türkçe", backup.Get(key)?.Text);
            using var current = new ArticleNoteStore(db);
            Assert.Equal("İkinci not", current.Get(key)?.Text);
            Assert.True(backup.CheckIntegrity());
            Assert.True(current.CheckIntegrity());
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void CandidatePdfReceiptNeverBecomesOfficialCertification()
    {
        string dir = NewDirectory();
        Directory.CreateDirectory(dir);
        try
        {
            string input = Path.Combine(dir, "synthetic.pdf");
            File.WriteAllText(input, "%PDF-1.4\nSENTETİK TEST İÇERİĞİ");
            var intake = new LocalPdfEvidenceStore(Path.Combine(dir, "store"));
            var receipt = intake.Import(input, "5237");
            Assert.Equal("5237", receipt.LawNumber);
            Assert.Equal(64, receipt.Sha256.Length);
            Assert.Equal(PdfEvidenceStatus.ByteIntegrityOnly, receipt.Status);
            Assert.False(receipt.OfficialFullTextVerified);
            Assert.False(receipt.ArticlePageMappingVerified);
            Assert.False(receipt.TemporalApplicabilityVerified);
            Assert.True(File.Exists(receipt.CandidateFile));
            Assert.True(File.Exists(receipt.ReceiptFile));
            Assert.Equal(receipt.Sha256, intake.Import(input, "5237").Sha256);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void InvalidOrOutOfScopeCandidateIsRejected()
    {
        string dir = NewDirectory();
        Directory.CreateDirectory(dir);
        try
        {
            string input = Path.Combine(dir, "not-pdf.txt");
            File.WriteAllText(input, "THIS IS NOT A PDF");
            var store = new LocalPdfEvidenceStore(Path.Combine(dir, "store"));
            Assert.Throws<InvalidDataException>(() => store.Import(input, "5237"));
            Assert.Throws<ArgumentException>(() => store.Import(input, "../5237"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
