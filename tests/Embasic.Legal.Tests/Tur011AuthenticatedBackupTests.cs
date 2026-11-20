using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur011AuthenticatedBackupTests
{
    private const string Password = "Synthetic-Only-Tur011-Passphrase!234";
    private static readonly ArticleNoteKey Key =
        new("5237", "134", "1", "SYNTHETIC-VERSION-HASH-ONLY");

    private static string TempRoot() =>
        Path.Combine(Path.GetTempPath(), "embasic-tur011-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void TamperedAesGcmNoteWithValidSqliteStructureMustBlockRestoreWithoutLosingTarget()
    {
        string root = TempRoot();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "vault.backup.sqlite3");
            using (var vault = new EncryptedArticleNotes(target, Password))
            {
                vault.Save(Key, "Original synthetic content");
                vault.BackupTo(backup);
                vault.Save(Key, "Newer current content");
            }
            // This manipulates a SQLite field, leaving quick_check: ok,
            // but invalidating the AES-GCM authentication tag.
            using (var tamper = new ArticleNoteStore(backup))
            {
                tamper.Save(Key, "EAL4:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                Assert.True(tamper.CheckIntegrity());
            }
            Assert.Throws<InvalidDataException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Password));
            using var unchanged = new EncryptedArticleNotes(target, Password);
            Assert.Equal("Newer current content", unchanged.Get(Key)?.Text);
            Assert.False(File.Exists(target + ".pre-restore.sqlite3"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void DamagedLiveEncryptedNoteBlocksBackupBeforeReplacingExistingCopy()
    {
        string root = TempRoot();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "vault-backup.sqlite3");
            using (var good = new EncryptedArticleNotes(target, Password))
            {
                good.Save(Key, "Original unmodified backup note");
                good.BackupTo(backup);
            }
            byte[] hashBefore = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(backup));
            using (var tamper = new ArticleNoteStore(target))
            {
                tamper.Save(Key, "EAL4:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                Assert.True(tamper.CheckIntegrity());
            }
            using (var damaged = new EncryptedArticleNotes(target, Password))
            {
                Assert.False(damaged.CheckIntegrity());
                Assert.Throws<InvalidDataException>(() => damaged.BackupTo(backup));
            }
            Assert.Equal(hashBefore,
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(backup)));
            using var original = new EncryptedArticleNotes(backup, Password);
            Assert.Equal("Original unmodified backup note", original.Get(Key)?.Text);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void VersionBoundAadTamperIsDetectedEvenThoughSqliteQuickCheckPasses()
    {
        string root = TempRoot();
        Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            using (var vault = new EncryptedArticleNotes(db, Password))
                vault.Save(Key, "Synthetic sensitive marker");
            using (var sql = new ArticleNoteStore(db))
            {
                var original = sql.Get(Key)!;
                sql.Save(Key with { VersionKey = "TAMPERED" }, original.Text);
                Assert.True(sql.CheckIntegrity());
            }
            using var damaged = new EncryptedArticleNotes(db, Password);
            Assert.False(damaged.CheckIntegrity());
        }
        finally { Directory.Delete(root, true); }
    }
}
