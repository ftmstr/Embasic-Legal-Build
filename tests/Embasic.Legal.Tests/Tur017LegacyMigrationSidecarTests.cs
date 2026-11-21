using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// Synthetic-only legacy plaintext SQLite sidecar gates. No real case files,
/// no synthetic UAT PASS, no deletion or checkpoint of a sidecar.
/// </summary>
public sealed class Tur017LegacyMigrationSidecarTests
{
    private const string Pass = "Synthetic-Only-TUR017-ReadOnly-Migration-Passphrase";
    private static readonly ArticleNoteKey Key = new(
        "5271", "134", "1", "SYNTHETIC-TUR017");
    private static string Root() => Path.Combine(Path.GetTempPath(),
        "embasic-tur017-" + Guid.NewGuid().ToString("N"));
    private static string Sha(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Theory]
    [InlineData("-journal")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    public void PreviewRejectsEveryKnownLegacySidecarWithoutModifyingSource(string suffix)
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "legacy.sqlite3");
            using (var old = new ArticleNoteStore(source))
                old.Save(Key, "SYNTHETIC LEGACY NOTE");
            string originalSha = Sha(source);
            string sidecar = source + suffix;
            byte[] evidence = [0x47, 0x45, 0x4e, 0x54, 0x52, 0x59];
            File.WriteAllBytes(sidecar, evidence);

            Assert.Throws<IOException>(() => LegacyNoteMigration.Preview(source));
            Assert.Equal(originalSha, Sha(source));
            Assert.Equal(evidence, File.ReadAllBytes(sidecar));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData("-journal")]
    [InlineData("-wal")]
    public void NewSidecarAfterApprovedPreviewRejectsImportAndPreservesTarget(string suffix)
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "legacy.sqlite3");
            string destination = Path.Combine(root, "vault.sqlite3");
            string rollback = Path.Combine(root, "rollback.sqlite3");
            using (var old = new ArticleNoteStore(source))
                old.Save(Key, "SYNTHETIC DO NOT IMPORT");
            var preview = LegacyNoteMigration.Preview(source);
            var initialHash = Sha(source);
            using var vault = new EncryptedArticleNotes(destination, Pass);
            var sidecar = source + suffix;
            byte[] evidence = [1, 2, 3, 4, 5, 6, 7, 8];
            File.WriteAllBytes(sidecar, evidence);

            Assert.Throws<IOException>(() =>
                LegacyNoteMigration.Apply(preview, vault, true, rollback));
            Assert.Null(vault.Get(Key));
            Assert.False(File.Exists(rollback));
            Assert.Equal(initialHash, Sha(source));
            Assert.Equal(evidence, File.ReadAllBytes(sidecar));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
