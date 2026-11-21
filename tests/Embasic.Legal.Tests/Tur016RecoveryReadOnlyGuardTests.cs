using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur016RecoveryReadOnlyGuardTests
{
    private const string Pass = "Synthetic-TUR016-ReadOnly-Only!";
    private static readonly ArticleNoteKey Key = new("5271", "134", "1", "SYNTHETIC-TUR016");
    private static string Root() => Path.Combine(Path.GetTempPath(), "embasic-tur016-" + Guid.NewGuid().ToString("N"));
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void ForeignExistingSqliteTargetCannotBeRewrittenByRestorePreview()
    {
        string root=Root(); Directory.CreateDirectory(root);
        try
        {
            string backup=Path.Combine(root,"valid-backup.sqlite3");
            string source=Path.Combine(root,"source.sqlite3");
            string target=Path.Combine(root,"foreign.sqlite3");
            using (var original = new EncryptedArticleNotes(source, Pass))
            {
                original.Save(Key,"SYNTHETIC SAFE");
                original.BackupTo(backup);
            }
            using (var raw = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource=target, Mode=SqliteOpenMode.ReadWriteCreate, Pooling=false
            }.ToString()))
            {
                raw.Open();
                using var cmd=raw.CreateCommand();
                cmd.CommandText="CREATE TABLE private_metadata (label TEXT); INSERT INTO private_metadata VALUES ('DO_NOT_CHANGE');";
                cmd.ExecuteNonQuery();
            }
            string sha=Hash(target);
            Assert.ThrowsAny<Exception>(()=>EncryptedArticleNotes.PreviewRestore(backup,target,Pass));
            Assert.Equal(sha,Hash(target));
            using (var verify = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource=target,Mode=SqliteOpenMode.ReadOnly,Pooling=false }.ToString()))
            {
                verify.Open();
                using var cmd=verify.CreateCommand();
                cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArticleNotes';";
                Assert.Equal(0L, (long)cmd.ExecuteScalar()!);
            }
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void ForeignExistingSqliteBackupCannotBeRewrittenByRestorePreview()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string target=Path.Combine(root,"target.sqlite3");
            string foreign=Path.Combine(root,"foreign-backup.sqlite3");
            using(var vault=new EncryptedArticleNotes(target,Pass)) vault.Save(Key,"PRESERVE");
            using(var raw=new SqliteConnection(new SqliteConnectionStringBuilder{
                DataSource=foreign,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString()))
            {
                raw.Open();
                using var cmd=raw.CreateCommand();
                cmd.CommandText="CREATE TABLE external (key TEXT);";cmd.ExecuteNonQuery();
            }
            string sourceHash=Hash(foreign), targetHash=Hash(target);
            Assert.ThrowsAny<Exception>(()=>EncryptedArticleNotes.PreviewRestore(foreign,target,Pass));
            Assert.Equal(sourceHash,Hash(foreign));
            Assert.Equal(targetHash,Hash(target));
            using var intact=new EncryptedArticleNotes(target,Pass);
            Assert.Equal("PRESERVE",intact.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void ValidBackupAndTargetHaveIdenticalBytesAfterReadOnlyPreview()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string db=Path.Combine(root,"vault.sqlite3");
            string backup=Path.Combine(root,"backup.sqlite3");
            using(var vault=new EncryptedArticleNotes(db,Pass))
            {
                vault.Save(Key,"BEFORE");vault.BackupTo(backup);vault.Save(Key,"AFTER");
            }
            string beforeBackup=Hash(backup),beforeDb=Hash(db);
            var preview=EncryptedArticleNotes.PreviewRestore(backup,db,Pass);
            Assert.True(preview.WouldDiscardCurrentData);
            Assert.Equal(beforeBackup,Hash(backup));
            Assert.Equal(beforeDb,Hash(db));
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void MissingBackupIsNeverCreatedByReadOnlyInspection()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string missing=Path.Combine(root,"missing.sqlite3");
            string target=Path.Combine(root,"vault.sqlite3");
            using(var vault=new EncryptedArticleNotes(target,Pass)) vault.Save(Key,"SYNTHETIC");
            Assert.Throws<FileNotFoundException>(()=>EncryptedArticleNotes.PreviewRestore(missing,target,Pass));
            Assert.False(File.Exists(missing));
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void ValidConfirmedRestorePreservesRollbackAndEncryption()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string target=Path.Combine(root,"vault.sqlite3"),backup=Path.Combine(root,"old.sqlite3");
            using(var vault=new EncryptedArticleNotes(target,Pass))
            {
                vault.Save(Key,"OLD");vault.BackupTo(backup);vault.Save(Key,"CURRENT");
            }
            var preview=EncryptedArticleNotes.PreviewRestore(backup,target,Pass);
            EncryptedArticleNotes.RestoreFrom(backup,target,Pass,true,preview);
            using var restored=new EncryptedArticleNotes(target,Pass);
            Assert.Equal("OLD",restored.Get(Key)?.Text);
            using var rollback=new EncryptedArticleNotes(target+".pre-restore.sqlite3",Pass);
            Assert.Equal("CURRENT",rollback.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }
}
