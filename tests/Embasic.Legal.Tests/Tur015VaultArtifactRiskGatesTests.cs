using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur015VaultArtifactRiskGatesTests
{
    private const string Pass = "Synthetic-Only-Privacy-TUR015!";
    private static readonly ArticleNoteKey Key =
        new("5271", "134", "2", "SYNTHETIC-2026");

    private static string NewRoot() => Path.Combine(Path.GetTempPath(),
        "embasic-tur015-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void WitnessInSameVaultDirectoryIsRejectedWithoutWritingAFile()
    {
        string root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            string local = Path.Combine(root, "receipt.json");
            string externalFolder = Path.Combine(root, "independent");
            Directory.CreateDirectory(externalFolder);
            string external = Path.Combine(externalFolder, "receipt.json");
            using var vault = new EncryptedArticleNotes(db, Pass);
            vault.Save(Key, "ONLY SYNTHETIC");
            Assert.Throws<IOException>(() => vault.ExportIntegrityWitness(local));
            Assert.False(File.Exists(local));
            vault.ExportIntegrityWitness(external);
            Assert.True(vault.VerifyIntegrityWitness(external));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TargetWalSidecarBlocksRestoreBeforeTargetChanges()
    {
        string root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "backup.sqlite3");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key, "ORIGINAL NOTE");
                vault.BackupTo(backup);
                vault.Save(Key, "MORE RECENT NOTE");
            }
            byte[] before = File.ReadAllBytes(target);
            string wal = target + "-wal";
            File.WriteAllText(wal, "SYNTHETIC: DO NOT TOUCH");
            Assert.Throws<IOException>(() =>
                EncryptedArticleNotes.PreviewRestore(backup, target, Pass));
            Assert.Throws<IOException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Pass,
                    userConfirmedDataLoss: true));
            Assert.Equal(before, File.ReadAllBytes(target));
            Assert.True(File.Exists(wal));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void BackupJournalSidecarBlocksRestoreAndDoesNotCreateTarget()
    {
        string root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "source.sqlite3");
            string backup = Path.Combine(root, "backup.sqlite3");
            string target = Path.Combine(root, "empty-target.sqlite3");
            using (var vault = new EncryptedArticleNotes(source, Pass))
            {
                vault.Save(Key, "ONLY SYNTHETIC");
                vault.BackupTo(backup);
            }
            File.WriteAllText(backup + "-journal", "SYNTHETIC");
            Assert.Throws<IOException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Pass));
            Assert.False(File.Exists(target));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void RawSqliteEncryptedNoteBackupStillDisclosesArticleMetadata()
    {
        string root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "backup.sqlite3");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(Key, "SECRET-SYNTHETIC-ONLY-TUR015");
                vault.BackupTo(backup);
            }
            string bytesAsLatin1 = System.Text.Encoding.Latin1.GetString(
                File.ReadAllBytes(backup));
            Assert.Contains("SYNTHETIC-2026", bytesAsLatin1);
            Assert.DoesNotContain("SECRET-SYNTHETIC-ONLY-TUR015", bytesAsLatin1);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void CombinedRollbackOfVaultAndReceiptIsNotDetectedByHmac()
    {
        string root = NewRoot();
        Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            string oldDb = Path.Combine(root, "old.sqlite3");
            string independent = Path.Combine(root, "outside");
            Directory.CreateDirectory(independent);
            string receipt = Path.Combine(independent, "latest.json");
            string oldReceipt = Path.Combine(independent, "old.json");
            using (var vault = new EncryptedArticleNotes(db, Pass))
            {
                vault.Save(Key, "OLD");
                vault.BackupTo(oldDb);
                vault.ExportIntegrityWitness(oldReceipt);
                vault.Save(Key, "NEW");
                vault.ExportIntegrityWitness(receipt);
            }
            File.Copy(oldDb, db, true);
            File.Copy(oldReceipt, receipt, true);
            using var rolledBack = new EncryptedArticleNotes(db, Pass);
            Assert.Equal("OLD", rolledBack.Get(Key)?.Text);
            Assert.True(rolledBack.VerifyIntegrityWitness(receipt));
            // This is a documented failure mode, NOT a trusted anti-rollback PASS.
        }
        finally { Directory.Delete(root, true); }
    }
}
