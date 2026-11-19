using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur004EncryptedNoteTests
{
    private static string Workspace() => Path.Combine(Path.GetTempPath(),
        "embasic-tur004-" + Guid.NewGuid().ToString("N"));
    private const string Password = "Test-Only-Passphrase-DoNotUse-Outside-CI!";
    private static readonly ArticleNoteKey Sample =
        new("5237", "6", "1-e", "SYNTHETIC_SOURCE_2026");

    [Fact]
    public void EncryptedRoundTripDoesNotStoreCleartextAndPinsVersion()
    {
        string root = Workspace();
        try
        {
            string path = Path.Combine(root, "vault.sqlite3");
            string noteId;
            using (var vault = new EncryptedArticleNotes(path, Password))
            {
                noteId = vault.Save(Sample, "ÖZEL SENTETİK NOT — ⚖").Id;
                Assert.True(vault.HasNote(Sample));
                Assert.False(vault.HasNote(Sample with { VersionKey = "OTHER_VERSION" }));
                Assert.Equal(noteId, vault.Save(Sample, "SENTETİK GÜNCEL NOT").Id);
            }
            using (var vault = new EncryptedArticleNotes(path, Password))
            {
                Assert.Equal("SENTETİK GÜNCEL NOT", vault.Get(Sample)?.Text);
                Assert.ThrowsAny<CryptographicException>(() =>
                    new EncryptedArticleNotes(path, "Incorrect-Long-Password-123"));
                Assert.True(vault.CheckIntegrity());
            }
            byte[] stored = File.ReadAllBytes(path);
            string visible = System.Text.Encoding.UTF8.GetString(stored);
            Assert.DoesNotContain("SENTETİK GÜNCEL NOT", visible);
            Assert.DoesNotContain("ÖZEL SENTETİK NOT", visible);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void AuthenticatedEncryptedBackupRestoresWithoutLosingCurrentState()
    {
        string root = Workspace();
        try
        {
            string path = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "vault.backup");
            using (var vault = new EncryptedArticleNotes(path, Password))
            {
                vault.Save(Sample, "Yedek öncesi sentetik not");
                vault.BackupTo(backup);
                vault.Save(Sample, "Yedek sonrası sentetik not");
            }
            Assert.ThrowsAny<CryptographicException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, path, "Incorrect-Long-Password-123"));
            using (var vault = new EncryptedArticleNotes(path, Password))
                Assert.Equal("Yedek sonrası sentetik not", vault.Get(Sample)?.Text);
            EncryptedArticleNotes.RestoreFrom(backup, path, Password);
            using (var vault = new EncryptedArticleNotes(path, Password))
                Assert.Equal("Yedek öncesi sentetik not", vault.Get(Sample)?.Text);
            string rollback = path + ".pre-restore.sqlite3";
            Assert.True(File.Exists(rollback));
            using (var previous = new EncryptedArticleNotes(rollback, Password))
                Assert.Equal("Yedek sonrası sentetik not", previous.Get(Sample)?.Text);
            Assert.Throws<IOException>(() => EncryptedArticleNotes.RestoreFrom(backup, path, Password));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void CannotRestoreFromDifferentVaultEvenWithSamePassword()
    {
        string root = Workspace();
        try
        {
            string target = Path.Combine(root, "target.sqlite3");
            string other = Path.Combine(root, "other.sqlite3");
            string backup = Path.Combine(root, "other.backup");
            using (var first = new EncryptedArticleNotes(target, Password))
                first.Save(Sample, "Korunacak not");
            using (var second = new EncryptedArticleNotes(other, Password))
            {
                second.Save(Sample, "Başka kasa");
                second.BackupTo(backup);
            }
            Assert.Throws<InvalidDataException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Password));
            using var safe = new EncryptedArticleNotes(target, Password);
            Assert.Equal("Korunacak not", safe.Get(Sample)?.Text);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ExistingPlaintextDatabaseIsNeverSilentlyUpgraded()
    {
        string root = Workspace();
        try
        {
            string target = Path.Combine(root, "legacy.sqlite3");
            using (var legacy = new ArticleNoteStore(target))
                legacy.Save(Sample, "Önceki sentetik düz metin korunmalıdır");
            Assert.Throws<InvalidDataException>(() => new EncryptedArticleNotes(target, Password));
            using var read = new ArticleNoteStore(target);
            Assert.Equal("Önceki sentetik düz metin korunmalıdır", read.Get(Sample)?.Text);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
