using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// Synthetic proof that stale sidecars block raw and AES-GCM backup replacement.
/// No physical Windows GUI, disk recovery, or plaintext-erasure PASS is claimed.
/// </summary>
public sealed class Tur018DestinationSidecarBackupTests
{
    private const string Pass = "TUR018-Synthetic-Only-Encrypted-Vault-Passphrase";
    private static readonly ArticleNoteKey Key = new("5271", "134", "1", "SYNTHETIC-TUR018");
    private static string Root() => Path.Combine(Path.GetTempPath(), "embasic-tur018-" + Guid.NewGuid().ToString("N"));
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static readonly byte[] Evidence = [0x54, 0x55, 0x52, 0x30, 0x31, 0x38];

    [Theory]
    [InlineData("-journal")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    public void EncryptedBackupDestinationWithSidecarPreservesOldBackupAndSidecar(string suffix)
    {
        string root=Root(); Directory.CreateDirectory(root);
        try
        {
            string source=Path.Combine(root,"vault.sqlite3");
            string backup=Path.Combine(root,"encrypted-copy.sqlite3");
            using var vault=new EncryptedArticleNotes(source,Pass);
            vault.Save(Key,"OLD SYNTHETIC");
            vault.BackupTo(backup);
            string savedSha=Sha(backup);
            vault.Save(Key,"NEW SYNTHETIC");
            File.WriteAllBytes(backup+suffix,Evidence);

            Assert.Throws<IOException>(() => vault.BackupTo(backup));
            Assert.Equal(savedSha,Sha(backup));
            Assert.Equal(Evidence,File.ReadAllBytes(backup+suffix));
            Assert.Empty(Directory.EnumerateFiles(root,"*.auth.tmp"));
            Assert.Equal("NEW SYNTHETIC",vault.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Theory]
    [InlineData("-journal")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    public void PlainSqliteBackupDestinationWithSidecarPreservesOldBackupAndSidecar(string suffix)
    {
        string root=Root(); Directory.CreateDirectory(root);
        try
        {
            string source=Path.Combine(root,"legacy.sqlite3");
            string backup=Path.Combine(root,"legacy-copy.sqlite3");
            using var store=new ArticleNoteStore(source);
            store.Save(Key,"OLD SYNTHETIC");
            store.BackupTo(backup);
            string savedSha=Sha(backup);
            store.Save(Key,"NEW SYNTHETIC");
            File.WriteAllBytes(backup+suffix,Evidence);

            Assert.Throws<IOException>(() => store.BackupTo(backup));
            Assert.Equal(savedSha,Sha(backup));
            Assert.Equal(Evidence,File.ReadAllBytes(backup+suffix));
            Assert.Empty(Directory.EnumerateFiles(root,"*.tmp"));
            Assert.Equal("NEW SYNTHETIC",store.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void CleanEncryptedBackupCanStillReplaceExistingCopyAndDecrypt()
    {
        string root=Root(); Directory.CreateDirectory(root);
        try
        {
            string source=Path.Combine(root,"vault.sqlite3");
            string backup=Path.Combine(root,"copy.sqlite3");
            using (var vault=new EncryptedArticleNotes(source,Pass))
            {
                vault.Save(Key,"OLDER");
                vault.BackupTo(backup);
                vault.Save(Key,"NEWER");
                vault.BackupTo(backup);
            }
            using var reopened=new EncryptedArticleNotes(backup,Pass);
            Assert.Equal("NEWER",reopened.Get(Key)?.Text);
            Assert.True(reopened.CheckIntegrity());
        }
        finally { Directory.Delete(root,true); }
    }
}
