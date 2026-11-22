using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Embasic.Legal.Tests;

/// <summary>
/// TUR021: an ordinary public encrypted-vault unlock must never modify a
/// foreign/invalid existing SQLite file before EAL4 identity/password check.
/// Synthetic-only, not an OS-level file lock, NTFS race or physical Windows UAT.
/// </summary>
public sealed class Tur021ReadOnlyPreAuthUnlockTests
{
    private const string Pass = "TUR021-Synthetic-Vault-Password-Only";
    private static readonly ArticleNoteKey Key =
        new("5271", "134", "1", "TUR021-SYNTHETIC");
    private static string Root() => Path.Combine(Path.GetTempPath(),
        "embasic-tur021-" + Guid.NewGuid().ToString("N"));
    private static string Hash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void ForeignSqliteMissingArticleNotesTableIsNotAlteredByNormalUnlock()
    {
        string root = Root(); Directory.CreateDirectory(root);
        try
        {
            string foreign = Path.Combine(root,"foreign.sqlite3");
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource=foreign, Mode=SqliteOpenMode.ReadWriteCreate, Pooling=false
            }.ToString()))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "CREATE TABLE external_records (label TEXT); INSERT INTO external_records VALUES ('DO_NOT_MODIFY');";
                cmd.ExecuteNonQuery();
            }
            string before = Hash(foreign);
            Assert.ThrowsAny<Exception>(() => new EncryptedArticleNotes(foreign,Pass));
            Assert.Equal(before,Hash(foreign));
            using var reader = new SqliteConnection(new SqliteConnectionStringBuilder{
                DataSource=foreign,Mode=SqliteOpenMode.ReadOnly,Pooling=false
            }.ToString());
            reader.Open();
            using var query = reader.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArticleNotes';";
            Assert.Equal(0L,(long)query.ExecuteScalar()!);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void LegacyArticleNotesWithoutVaultSentinelIsNotAlteredOnUnlock()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string legacy=Path.Combine(root,"legacy.sqlite3");
            using (var original=new ArticleNoteStore(legacy))
                original.Save(Key,"SYNTHETIC LEGACY TEXT");
            string before=Hash(legacy);
            Assert.Throws<InvalidDataException>(()=>new EncryptedArticleNotes(legacy,Pass));
            Assert.Equal(before,Hash(legacy));
            using var unchanged=new ArticleNoteStore(legacy,readOnly:true);
            Assert.Equal("SYNTHETIC LEGACY TEXT",unchanged.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void WrongPasswordCannotChangeValidVaultMainFileOrEncryptedNote()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"vault.sqlite3");
            using(var original=new EncryptedArticleNotes(path,Pass))
                original.Save(Key,"SYNTHETIC ENCRYPTED TEXT");
            string before=Hash(path);
            Assert.Throws<CryptographicException>(()=>
                new EncryptedArticleNotes(path,"OTHER-SYNTHETIC-PASSWORD"));
            Assert.Equal(before,Hash(path));
            using var legitimate=new EncryptedArticleNotes(path,Pass);
            Assert.Equal("SYNTHETIC ENCRYPTED TEXT",legitimate.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void ValidExistingVaultOpensAndCanSaveAfterReadOnlyAuthentication()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"vault.sqlite3");
            using(var initial=new EncryptedArticleNotes(path,Pass))
                initial.Save(Key,"OLD");
            using(var writable=new EncryptedArticleNotes(path,Pass))
            {
                Assert.Equal("OLD",writable.Get(Key)?.Text);
                writable.Save(Key,"NEW");
                Assert.True(writable.CheckIntegrity());
            }
            using var verify=new EncryptedArticleNotes(path,Pass);
            Assert.Equal("NEW",verify.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void MissingVaultStillCreatesItsOwnVerifiedEncryptedStore()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"new","vault.sqlite3");
            using(var created=new EncryptedArticleNotes(path,Pass))
            {
                created.Save(Key,"FRESH SYNTHETIC");
                Assert.True(created.CheckIntegrity());
            }
            Assert.True(File.Exists(path));
            using var opened=new EncryptedArticleNotes(path,Pass);
            Assert.Equal("FRESH SYNTHETIC",opened.Get(Key)?.Text);
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void CorruptedEncryptedUserNoteIsStillRecognizedAsVaultButBackupIsRefused()
    {
        string root=Root();Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"vault.sqlite3"),backup=Path.Combine(root,"backup.sqlite3");
            using(var vault=new EncryptedArticleNotes(path,Pass))
                vault.Save(Key,"ONLY SYNTHETIC");
            using(var tamper=new ArticleNoteStore(path))
                tamper.Save(Key,"EAL4:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            using(var reopened=new EncryptedArticleNotes(path,Pass))
            {
                Assert.False(reopened.CheckIntegrity());
                Assert.Throws<InvalidDataException>(() => reopened.BackupTo(backup));
            }
            Assert.False(File.Exists(backup));
        }
        finally { Directory.Delete(root,true); }
    }
}
