using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// TUR020 synthetic sidecar ENTRY type tests. Not a filesystem-lock or
/// physical Windows, junction, hardlink, or hostile TOCTOU acceptance test.
/// </summary>
public sealed class Tur020SidecarEntryFailClosedTests
{
    private const string Pass = "TUR020-SYNTHETIC-ONLY-VAULT-PASSWORD";
    private static readonly ArticleNoteKey Key =
        new("5271", "134", "SYNTHETIC", "TUR020-V1");
    private static string Root() => Path.Combine(Path.GetTempPath(),
        "embasic-tur020-" + Guid.NewGuid().ToString("N"));
    private static string Sha(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Theory]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void DirectoryEntryOccupyingSidecarNameFailsClosed(string suffix)
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            Directory.CreateDirectory(db + suffix);
            Assert.False(File.Exists(db + suffix)); // TUR019 guard missed it
            Assert.True(Directory.Exists(db + suffix));
            Assert.Throws<IOException>(() =>
                VaultArtifactRiskGates.RequireNoSqliteSidecars(db));
            Assert.True(Directory.Exists(db + suffix));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void EncryptedBackupPreservesExistingBackupOnDirectorySidecar()
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string db = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "backup.sqlite3");
            using var vault = new EncryptedArticleNotes(db, Pass);
            vault.Save(Key, "SYNTHETIC-OLD");
            vault.BackupTo(backup);
            string original = Sha(backup);
            vault.Save(Key, "SYNTHETIC-NEW");
            Directory.CreateDirectory(backup + "-journal");

            Assert.Throws<IOException>(() => vault.BackupTo(backup));
            Assert.Equal(original, Sha(backup));
            Assert.True(Directory.Exists(backup + "-journal"));
            Assert.Equal("SYNTHETIC-NEW", vault.Get(Key)?.Text);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void RestorePreviewAndRestoreRejectSidecarDirectoryWithoutTargetChanges()
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "target.sqlite3");
            string backup = Path.Combine(root, "backup.sqlite3");
            using (var vault = new EncryptedArticleNotes(target, Pass))
            {
                vault.Save(Key, "SYNTHETIC-BEFORE");
                vault.BackupTo(backup);
                vault.Save(Key, "SYNTHETIC-AFTER");
            }
            string original = Sha(target);
            Directory.CreateDirectory(target + "-wal");
            Assert.Throws<IOException>(() =>
                EncryptedArticleNotes.PreviewRestore(backup, target, Pass));
            Assert.Throws<IOException>(() =>
                EncryptedArticleNotes.RestoreFrom(backup, target, Pass,
                    userConfirmedDataLoss: true));
            Assert.Equal(original, Sha(target));
            Assert.True(Directory.Exists(target + "-wal"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void LegacyPlaintextPreviewRejectsDirectoryAtSidecarName()
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "legacy.sqlite3");
            using (var legacy = new ArticleNoteStore(source))
                legacy.Save(Key, "SYNTHETIC-PLAIN");
            string original = Sha(source);
            Directory.CreateDirectory(source + "-shm");
            Assert.Throws<IOException>(() => LegacyNoteMigration.Preview(source));
            Assert.Equal(original, Sha(source));
            Assert.True(Directory.Exists(source + "-shm"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void NotYetCreatedDestinationAndSidecarsAreNotFalsePositives()
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string future = Path.Combine(root, "future-folder", "new.sqlite3");
            VaultArtifactRiskGates.RequireNoSqliteSidecars(future);
            Assert.False(File.Exists(future));
            Assert.False(Directory.Exists(Path.GetDirectoryName(future)!));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
