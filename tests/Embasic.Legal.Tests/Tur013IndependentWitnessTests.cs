using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur013IndependentWitnessTests
{
    private const string Pass = "Synthetic-Only-External-Witness-Tur013!";
    private static readonly ArticleNoteKey A =
        new("5237", "134", "1", "SYNTHETIC-VERSION-2026");
    private static readonly ArticleNoteKey B =
        new("5271", "127", "3", "SYNTHETIC-VERSION-2026");

    private static string Root() => Path.Combine(
        Path.GetTempPath(), "embasic-tur013-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void WitnessContainsNoNoteBodiesOrLawIdentifiersAndVerifiesOnSameSnapshot()
    {
        string dir = Root();
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "independent"));
        try
        {
            string db = Path.Combine(dir, "vault.sqlite3");
            string external = Path.Combine(dir, "independent", "outside-vault.witness.json");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(A, "SENTETİK ÖZEL İÇERİK BİR");
                vault.Save(B, "SENTETİK ÖZEL İÇERİK İKİ");
                var receipt = vault.ExportIntegrityWitness(external);
                Assert.Equal(2, receipt.NoteCount);
                Assert.True(vault.VerifyIntegrityWitness(external));
                Assert.Throws<IOException>(() => vault.ExportIntegrityWitness(external));
            }
            string json = File.ReadAllText(external);
            Assert.DoesNotContain("SENTETİK ÖZEL", json);
            Assert.DoesNotContain("5271", json);
            Assert.DoesNotContain("5237", json);
            using var reopened = new EncryptedArticleNotes(db, Pass);
            Assert.True(reopened.VerifyIntegrityWitness(external));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DeletingEntireAuthenticatedRowIsInvisibleToAesGcmButWitnessDetectsIt()
    {
        string dir = Root();
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "independent"));
        try
        {
            string db = Path.Combine(dir, "vault.sqlite3");
            string external = Path.Combine(dir, "independent", "outside-vault.witness.json");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(A, "SYNTHETIC FIRST");
                vault.Save(B, "SYNTHETIC SECOND");
                vault.ExportIntegrityWitness(external);
            }
            using (var raw = new ArticleNoteStore(db))
            {
                Assert.True(raw.Delete(B));
                Assert.True(raw.CheckIntegrity());
            }
            using var restored = new EncryptedArticleNotes(db, Pass);
            Assert.True(restored.CheckIntegrity());  // All remaining rows have valid GCM tags.
            Assert.False(restored.VerifyIntegrityWitness(external));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ReplacingCurrentVaultWithOldValidBackupFailsIndependentWitnessCheck()
    {
        string dir = Root();
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "independent"));
        try
        {
            string db = Path.Combine(dir, "vault.sqlite3");
            string oldBackup = Path.Combine(dir, "old.backup.sqlite3");
            string external = Path.Combine(dir, "independent", "offline.witness.json");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(A, "OLD");
                vault.BackupTo(oldBackup);
                vault.Save(A, "NEW");
                vault.ExportIntegrityWitness(external);
            }
            File.Copy(oldBackup, db, overwrite: true);
            using var replaced = new EncryptedArticleNotes(db, Pass);
            Assert.True(replaced.CheckIntegrity());  // Old vault is cryptographically intact.
            Assert.False(replaced.VerifyIntegrityWitness(external));
            Assert.Equal("OLD", replaced.Get(A)?.Text);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void MetadataRevisionOrReceiptTamperingDoesNotPassIndependentMac()
    {
        string dir = Root();
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "independent"));
        try
        {
            string db = Path.Combine(dir, "vault.sqlite3");
            string external = Path.Combine(dir, "independent", "offline.witness.json");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(A, "RETAIN");
                vault.ExportIntegrityWitness(external);
                string text = File.ReadAllText(external);
                File.WriteAllText(external, text.Replace("\"NoteCount\":1", "\"NoteCount\":2"));
                Assert.False(vault.VerifyIntegrityWitness(external));
                vault.ExportIntegrityWitness(external, userConfirmedOverwrite: true);
            }
            using (var raw = new ArticleNoteStore(db))
            {
                var existing = raw.Get(A)!;
                raw.Save(A, existing.Text); // valid ciphertext, but timestamp changed
                Assert.True(raw.CheckIntegrity());
            }
            using var check = new EncryptedArticleNotes(db, Pass);
            Assert.True(check.CheckIntegrity());
            Assert.False(check.VerifyIntegrityWitness(external));
        }
        finally { Directory.Delete(dir, true); }
    }
}
