using System.Security.Cryptography;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur005LegacyMigrationTests
{
    private const string Pass = "SYNTHETIC-TEST-PASSPHRASE-ONLY-2026";
    private static readonly ArticleNoteKey Key =
        new("5237", "6", "1-e", "SYNTHETIC-OLD-LAW-V1");
    private static string DirectoryPath() =>
        Path.Combine(Path.GetTempPath(), "embasic-tur005-"+Guid.NewGuid().ToString("N"));

    [Fact]
    public void ConsentIsRequiredAndAtomicMigrationPreservesIdentityAndOldDatabase()
    {
        var root = DirectoryPath();
        try
        {
            string legacy = Path.Combine(root, "legacy.sqlite3");
            string vault = Path.Combine(root, "vault.sqlite3");
            string backup = Path.Combine(root, "rollback.sqlite3");
            ArticleNote original;
            using (var old = new ArticleNoteStore(legacy))
            {
                original = old.Save(Key, "SENTETİK Türkçe not ⚖");
                old.Save(Key with { VersionKey = "SYNTHETIC-OLD-LAW-V2" }, "İkinci sürüm");
            }
            var bytes = SHA256.HashData(File.ReadAllBytes(legacy));
            using (var encrypted = new EncryptedArticleNotes(vault, Pass))
            {
                var preview = LegacyNoteMigration.Preview(legacy);
                Assert.Equal(2, preview.NoteCount);
                Assert.False(File.Exists(backup));
                Assert.Throws<InvalidOperationException>(() =>
                    LegacyNoteMigration.Apply(preview, encrypted, false, backup));
                Assert.Null(encrypted.Get(Key));
                var migrated = LegacyNoteMigration.Apply(preview, encrypted, true, backup);
                Assert.Equal(2, migrated.ImportedNotes);
                Assert.True(File.Exists(migrated.EncryptedPreMigrationBackup));
                Assert.Equal(original.Id, encrypted.Get(Key)?.Id);
                Assert.Equal(original.CreatedUtc, encrypted.Get(Key)?.CreatedUtc);
                Assert.Equal(original.Text, encrypted.Get(Key)?.Text);
                Assert.Null(encrypted.Get(Key with { VersionKey = "SYNTHETIC-NEW-LAW-V3" }));
            }
            Assert.Equal(bytes, SHA256.HashData(File.ReadAllBytes(legacy)));
            using (var old = new ArticleNoteStore(legacy))
                Assert.Equal(original.Text, old.Get(Key)?.Text);
            using var restore = new EncryptedArticleNotes(backup, Pass);
            Assert.Null(restore.Get(Key)); // rollback snapshot preceded import
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void CollisionCannotOverwriteAnExistingEncryptedNote()
    {
        string root = DirectoryPath();
        try
        {
            string legacy = Path.Combine(root,"old.sqlite3");
            string vault = Path.Combine(root,"encrypted.sqlite3");
            string backup = Path.Combine(root,"no-backup.sqlite3");
            using (var old = new ArticleNoteStore(legacy))
            {
                old.Save(Key, "Eski kayıt");
                old.Save(Key with { ArticleKey = "7" }, "Diğer kayıt");
            }
            using var target = new EncryptedArticleNotes(vault, Pass);
            target.Save(Key, "Mevcut şifreli kayıt");
            var preview = LegacyNoteMigration.Preview(legacy);
            Assert.Throws<InvalidOperationException>(() =>
                LegacyNoteMigration.Apply(preview, target, true, backup));
            Assert.Equal("Mevcut şifreli kayıt", target.Get(Key)?.Text);
            Assert.Null(target.Get(Key with { ArticleKey = "7" }));
            Assert.False(File.Exists(backup));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ChangedSourceSincePreviewCannotBeImported()
    {
        string root = DirectoryPath();
        try
        {
            string legacy = Path.Combine(root, "source.sqlite3");
            string vault = Path.Combine(root, "vault.sqlite3");
            using (var old = new ArticleNoteStore(legacy))
                old.Save(Key, "İlk örnek");
            var preview = LegacyNoteMigration.Preview(legacy);
            using (var old = new ArticleNoteStore(legacy))
                old.Save(Key, "Değişmiş örnek");
            using var target = new EncryptedArticleNotes(vault, Pass);
            Assert.Throws<InvalidDataException>(() =>
                LegacyNoteMigration.Apply(preview, target, true,
                    Path.Combine(root,"backup.sqlite3")));
            Assert.Null(target.Get(Key));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
