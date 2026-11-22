using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// TUR023: pre-restore rollback destination must be entirely unused, including
/// orphan SQLite sidecar names; synthetic data only, not hostile NTFS TOCTOU.
/// </summary>
public sealed class Tur023RestoreRollbackSidecarTests
{
    private const string Pass = "TUR023-Synthetic-Rollback-Only-Password";
    private static readonly ArticleNoteKey Key =
        new("5271", "134", "SYNT", "TUR023-V1");

    private static string Root() => Path.Combine(
        Path.GetTempPath(), "embasic-tur023-" + Guid.NewGuid().ToString("N"));
    private static string Sha(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static (string Target,string Backup) Prepare(string root)
    {
        string target = Path.Combine(root, "vault.sqlite3");
        string backup = Path.Combine(root, "backup.sqlite3");
        using (var vault = new EncryptedArticleNotes(target,Pass))
        {
            vault.Save(Key,"SYNTHETIC-OLD");
            vault.BackupTo(backup);
            vault.Save(Key,"SYNTHETIC-CURRENT");
        }
        return (target,backup);
    }

    [Theory]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void OrphanRollbackSidecarRefusesRestoreAndPreservesAllBytes(string suffix)
    {
        string root=Root(); Directory.CreateDirectory(root);
        try
        {
            var (target, backup)=Prepare(root);
            var approved=EncryptedArticleNotes.PreviewRestore(backup,target,Pass);
            Assert.True(approved.WouldDiscardCurrentData);
            string rollback=target+".pre-restore.sqlite3";
            string orphan=rollback+suffix;
            byte[] proof=System.Text.Encoding.UTF8.GetBytes("SYNTHETIC-EVIDENCE-NOT-DELETE");
            File.WriteAllBytes(orphan,proof);
            string targetSha=Sha(target),backupSha=Sha(backup);
            Assert.Throws<IOException>(()=>EncryptedArticleNotes.RestoreFrom(
                backup,target,Pass,userConfirmedDataLoss:true,approvedPreview:approved));
            Assert.Equal(targetSha,Sha(target));
            Assert.Equal(backupSha,Sha(backup));
            Assert.Equal(proof,File.ReadAllBytes(orphan));
            Assert.False(File.Exists(rollback));
            using var valid=new EncryptedArticleNotes(target,Pass);
            Assert.Equal("SYNTHETIC-CURRENT",valid.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,recursive:true); }
    }

    [Fact]
    public void RollbackPathOccupiedByDirectoryAlsoRefusesRestoreUnchanged()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            var (target,backup)=Prepare(root);
            var preview=EncryptedArticleNotes.PreviewRestore(backup,target,Pass);
            string rollback=target+".pre-restore.sqlite3";
            Directory.CreateDirectory(rollback);
            string sourceSha=Sha(backup),targetSha=Sha(target);
            Assert.False(File.Exists(rollback));
            Assert.Throws<IOException>(()=>EncryptedArticleNotes.RestoreFrom(
                backup,target,Pass,true,preview));
            Assert.True(Directory.Exists(rollback));
            Assert.Equal(sourceSha,Sha(backup));
            Assert.Equal(targetSha,Sha(target));
        }
        finally {Directory.Delete(root,recursive:true);}
    }

    [Fact]
    public void CleanRollbackDestinationStillPreservesCurrentAndRestoresBackup()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            var (target,backup)=Prepare(root);
            var preview=EncryptedArticleNotes.PreviewRestore(backup,target,Pass);
            Assert.True(preview.WouldDiscardCurrentData);
            string rollback=target+".pre-restore.sqlite3";
            VaultArtifactRiskGates.RequireUnusedRollbackDestination(rollback);
            var result=EncryptedArticleNotes.RestoreFrom(backup,target,Pass,true,preview);
            Assert.Equal(target,result);
            Assert.True(File.Exists(rollback));
            using var restored=new EncryptedArticleNotes(target,Pass);
            using var prior=new EncryptedArticleNotes(rollback,Pass);
            Assert.Equal("SYNTHETIC-OLD",restored.Get(Key)?.Text);
            Assert.Equal("SYNTHETIC-CURRENT",prior.Get(Key)?.Text);
        }
        finally {Directory.Delete(root,recursive:true);}
    }
}
