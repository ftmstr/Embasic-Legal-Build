using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// TUR022: normal EAL4 Unlock/Create has to gate SQLite sidecar entries BEFORE
/// opening SQLite, not just the existing Backup/Restore/Migration workflows.
/// Synthetic data only; no file-handle locking/NTFS race proof or physical UAT.
/// </summary>
public sealed class Tur022VaultUnlockSidecarEntryTests
{
    private const string Pass = "TUR022-Synthetic-Password-For-Only-Testing";
    private static readonly ArticleNoteKey Key =
        new("5271", "134", "SYNTHETIC", "TUR022-V1");
    private static string Root() => Path.Combine(Path.GetTempPath(),
        "embasic-tur022-" + Guid.NewGuid().ToString("N"));
    private static string Hash(string p) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));

    [Theory]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void ExistingEncryptedVaultRejectsNamedSidecarFileBeforeUnlock(string suffix)
    {
        string dir = Root(); Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "vault.sqlite3");
            using (var first = new EncryptedArticleNotes(path, Pass))
                first.Save(Key, "SYNTHETIC ENCRYPTED BODY");
            byte[] marker = System.Text.Encoding.UTF8.GetBytes("DO-NOT-DELETE-EVIDENCE");
            string sidecar = path + suffix;
            File.WriteAllBytes(sidecar, marker);
            string before = Hash(path);
            Assert.Throws<IOException>(() => new EncryptedArticleNotes(path, Pass));
            Assert.Equal(before, Hash(path));
            Assert.Equal(marker, File.ReadAllBytes(sidecar));
            Assert.True(File.Exists(sidecar));
            // Once safely moved out of the synthetic test folder, the vault
            // remains verifiable; production code never deletes this sidecar.
            File.Delete(sidecar);
            using var verify = new EncryptedArticleNotes(path, Pass);
            Assert.Equal("SYNTHETIC ENCRYPTED BODY", verify.Get(Key)?.Text);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void DirectoryAtSidecarNameAlsoRefusesOrdinaryUnlockWithoutMutation()
    {
        string dir=Root(); Directory.CreateDirectory(dir);
        try
        {
            string path=Path.Combine(dir,"vault.sqlite3");
            using (var initial=new EncryptedArticleNotes(path,Pass))
                initial.Save(Key,"SYNTHETIC NOTE");
            string before=Hash(path);
            Directory.CreateDirectory(path+"-shm");
            Assert.Throws<IOException>(()=>new EncryptedArticleNotes(path,Pass));
            Assert.Equal(before,Hash(path));
            Assert.True(Directory.Exists(path+"-shm"));
        }
        finally { Directory.Delete(dir,recursive:true); }
    }

    [Fact]
    public void FreshVaultWithOrphanSidecarIsNotCreated()
    {
        string dir=Root(); Directory.CreateDirectory(dir);
        try
        {
            string path=Path.Combine(dir,"new-vault.sqlite3");
            string sidecar=path+"-journal";
            File.WriteAllText(sidecar,"SYNTHETIC-ORPHAN-JOURNAL");
            Assert.Throws<IOException>(()=>new EncryptedArticleNotes(path,Pass));
            Assert.False(File.Exists(path));
            Assert.Equal("SYNTHETIC-ORPHAN-JOURNAL",File.ReadAllText(sidecar));
        }
        finally { Directory.Delete(dir,recursive:true); }
    }

    [Fact]
    public void NormalVaultWithoutSidecarsStillCreatesUnlocksAndSaves()
    {
        string dir=Root(); Directory.CreateDirectory(dir);
        try
        {
            string path=Path.Combine(dir,"vault.sqlite3");
            using (var initial=new EncryptedArticleNotes(path,Pass))
            {
                initial.Save(Key,"BEFORE");
                Assert.True(initial.CheckIntegrity());
            }
            using (var reopened=new EncryptedArticleNotes(path,Pass))
            {
                Assert.Equal("BEFORE",reopened.Get(Key)?.Text);
                reopened.Save(Key,"AFTER");
            }
            using var final=new EncryptedArticleNotes(path,Pass);
            Assert.Equal("AFTER",final.Get(Key)?.Text);
        }
        finally { Directory.Delete(dir,recursive:true); }
    }
}
