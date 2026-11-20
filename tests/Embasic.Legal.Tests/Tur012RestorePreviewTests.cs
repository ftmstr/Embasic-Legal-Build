using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur012RestorePreviewTests
{
    private const string Pass = "Only-Synthetic-Tur012-Passphrase-!234";
    private static readonly ArticleNoteKey Key1 =
        new("5237", "134", "", "SYNTHETIC-2026");
    private static readonly ArticleNoteKey Key2 =
        new("5271", "127", "3", "SYNTHETIC-2026");

    private static string Root() => Path.Combine(Path.GetTempPath(),
        "embasic-tur012-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void RestoreOlderNoteMustBeExplicitlyConfirmedAndKeepRollback()
    {
        string root = Root();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.db");
            string backup = Path.Combine(root, "old.backup.db");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key1, "Older synthetic value");
                vault.BackupTo(backup);
                vault.Save(Key1, "Newer synthetic value");
            }
            var risk = EncryptedArticleNotes.PreviewRestore(backup, target, Pass);
            Assert.True(risk.WouldDiscardCurrentData);
            Assert.Equal(1, risk.ChangedNotes);
            Assert.Equal(0, risk.MissingFromBackup);
            Assert.Throws<InvalidOperationException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Pass));
            using (var intact = new EncryptedArticleNotes(target, Pass))
                Assert.Equal("Newer synthetic value", intact.Get(Key1)?.Text);
            EncryptedArticleNotes.RestoreFrom(backup, target, Pass,
                userConfirmedDataLoss: true, approvedPreview: risk);
            using var restored = new EncryptedArticleNotes(target, Pass);
            Assert.Equal("Older synthetic value", restored.Get(Key1)?.Text);
            using var rollback = new EncryptedArticleNotes(target + ".pre-restore.sqlite3", Pass);
            Assert.Equal("Newer synthetic value", rollback.Get(Key1)?.Text);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void PreviouslyAddedNoteMissingFromBackupMustRequireConsent()
    {
        string root = Root();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.db");
            string backup = Path.Combine(root, "old.backup.db");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key1, "First synthetic note");
                vault.BackupTo(backup);
                vault.Save(Key2, "Second synthetic note, not in old backup");
            }
            var preview = EncryptedArticleNotes.PreviewRestore(backup, target, Pass);
            Assert.Equal(1, preview.MissingFromBackup);
            Assert.True(preview.WouldDiscardCurrentData);
            Assert.Throws<InvalidOperationException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Pass));
            using var preserved = new EncryptedArticleNotes(target, Pass);
            Assert.Equal("Second synthetic note, not in old backup", preserved.Get(Key2)?.Text);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void AfterPreviewTargetMutationFailsEvenWithExplicitConsent()
    {
        string root = Root();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.db");
            string backup = Path.Combine(root, "old.backup.db");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key1, "Before");
                vault.BackupTo(backup);
                vault.Save(Key1, "After");
            }
            var preview = EncryptedArticleNotes.PreviewRestore(backup, target, Pass);
            using (var vault = new EncryptedArticleNotes(target, Pass))
                vault.Save(Key1, "After preview, must not be lost");
            Assert.Throws<IOException>(() => EncryptedArticleNotes.RestoreFrom(
                backup, target, Pass, userConfirmedDataLoss: true, approvedPreview: preview));
            using var safe = new EncryptedArticleNotes(target, Pass);
            Assert.Equal("After preview, must not be lost", safe.Get(Key1)?.Text);
            Assert.False(File.Exists(target + ".pre-restore.sqlite3"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void NonDestructiveRecoveryToNewTargetRequiresNoDataLossConsent()
    {
        string root = Root();
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "vault.db");
            string backup = Path.Combine(root, "old.backup.db");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key1, "Synthetic baseline");
                vault.BackupTo(backup);
            }
            File.Delete(target);
            var preview = EncryptedArticleNotes.PreviewRestore(backup, target, Pass);
            Assert.False(preview.WouldDiscardCurrentData);
            Assert.Null(preview.TargetSha256);
            EncryptedArticleNotes.RestoreFrom(backup, target, Pass, approvedPreview: preview);
            using var recovered = new EncryptedArticleNotes(target, Pass);
            Assert.Equal("Synthetic baseline", recovered.Get(Key1)?.Text);
        }
        finally { Directory.Delete(root, true); }
    }
}
