using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Embasic.Legal.Infrastructure;

/// <summary>
/// Preview contains metadata only. Legacy source is read only; source bytes and identity
/// are rechecked immediately before an explicitly consented, one-transaction import.
/// No plaintext export, no deletion of legacy files, no automatic migration.
/// </summary>
public sealed record LegacyMigrationPreview(
    string SourcePath,
    string SourceSha256,
    int NoteCount,
    long TotalUtf8Bytes,
    DateTimeOffset InspectedAtUtc);

public sealed record LegacyMigrationResult(
    int ImportedNotes, string SourceSha256, string EncryptedPreMigrationBackup);

public static class LegacyNoteMigration
{
    private const int MaxNotes = 1000;
    private const long MaxDatabaseBytes = 25L * 1024 * 1024;
    private const long MaxTotalTextBytes = 12L * 1024 * 1024;

    public static LegacyMigrationPreview Preview(string legacyPath)
    {
        string path = ValidPath(legacyPath);
        string before = Hash(path);
        var data = LoadReadOnly(path);
        // SQLite rollback journals can contain uncommitted plaintext not
        // reflected in the primary-file SHA-256. Treat them just like WAL/SHM.
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);
        string after = Hash(path);
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);
        if (!before.Equals(after, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Ön inceleme sırasında eski veritabanı değişti; aktarım reddedildi.");
        return new LegacyMigrationPreview(path, before, data.Count,
            data.Sum(n => (long)Encoding.UTF8.GetByteCount(n.Text)), DateTimeOffset.UtcNow);
    }

    public static LegacyMigrationResult Apply(
        LegacyMigrationPreview preview,
        EncryptedArticleNotes unlockedVault,
        bool userConsented,
        string immutableBackupPath)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(unlockedVault);
        if (!userConsented)
            throw new InvalidOperationException("Açık kullanıcı onayı olmadan aktarım yapılamaz.");
        string path = ValidPath(preview.SourcePath);
        if (!Hash(path).Equals(preview.SourceSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Eski kaynak ön incelemeden sonra değişti.");
        var notes = LoadReadOnly(path);
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);
        if (notes.Count == 0 || notes.Count != preview.NoteCount ||
            notes.Sum(n => (long)Encoding.UTF8.GetByteCount(n.Text)) != preview.TotalUtf8Bytes ||
            !Hash(path).Equals(preview.SourceSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Kaynak veya kapsam değişti; yeniden ön inceleme gerekir.");
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);

        foreach (var note in notes)
        {
            if (unlockedVault.HasNote(note.Key))
                throw new InvalidOperationException("Şifreli kasada çakışan not var; üzerine yazma yasak.");
        }

        string backup = Path.GetFullPath(immutableBackupPath);
        if (File.Exists(backup)) throw new IOException("Önceki geri alma yedeği mevcut; ezilmeyecek.");
        // Stop if a SQLite sidecar appeared before creating a rollback snapshot.
        // No deletion/checkpoint: source data stays available for human inspection.
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);
        unlockedVault.BackupTo(backup);
        if (!File.Exists(backup)) throw new IOException("Şifreli geri alma yedeği oluşmadı.");

        // The source is checked again after snapshotting target. This narrows but
        // does NOT eliminate OS-level TOCTOU without a trusted file lock/snapshot.
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);
        if (!Hash(path).Equals(preview.SourceSha256, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Geri alma yedeği sonrasında eski kaynak değişti; aktarım reddedildi.");
        VaultArtifactRiskGates.RequireNoSqliteSidecars(path);

        // The target INSERT is atomic and never upserts. Legacy file stays untouched.
        unlockedVault.ImportNewNotesAtomically(notes);
        foreach (var note in notes)
        {
            var actual = unlockedVault.Get(note.Key);
            if (actual?.Id != note.Id || !string.Equals(actual.Text, note.Text, StringComparison.Ordinal))
                throw new IOException("Aktarım sonu doğrulama uyuşmadı. Geri alma yedeği korunur.");
        }
        return new LegacyMigrationResult(notes.Count, preview.SourceSha256, backup);
    }

    private static string ValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Eski veritabanı yolu boş.");
        string full = Path.GetFullPath(path);
        var file = new FileInfo(full);
        if (!file.Exists || file.Length < 128 || file.Length > MaxDatabaseBytes)
            throw new InvalidDataException("Kaynak yok veya boyut sınırı dışında.");
        // TUR017: -journal was previously omitted. Presence alone blocks the
        // import, irrespective of primary SQLite file size or unchanged hash.
        // Never delete or auto-checkpoint a source sidecar.
        VaultArtifactRiskGates.RequireNoSqliteSidecars(full);
        return full;
    }

    private static string Hash(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static IReadOnlyList<ArticleNote> LoadReadOnly(string source)
    {
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = source,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        using var connection = new SqliteConnection(cs.ToString());
        connection.Open();
        using (var readOnly = connection.CreateCommand())
        {
            readOnly.CommandText = "PRAGMA query_only=ON;";
            readOnly.ExecuteNonQuery();
        }
        using (var integrity = connection.CreateCommand())
        {
            integrity.CommandText = "PRAGMA quick_check;";
            if (!string.Equals(Convert.ToString(integrity.ExecuteScalar(),
                    CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Eski SQLite bütünlüğü başarısız.");
        }
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, LawNumber, ArticleKey, SubdivisionKey, VersionKey,
                   NoteText, CreatedUtc, UpdatedUtc
            FROM ArticleNotes
            ORDER BY LawNumber, ArticleKey, SubdivisionKey, VersionKey;
            """;
        using var reader = command.ExecuteReader();
        var notes = new List<ArticleNote>();
        var identities = new HashSet<ArticleNoteKey>();
        long total = 0;
        while (reader.Read())
        {
            if (notes.Count >= MaxNotes)
                throw new InvalidDataException("Eski kaynak 1000 not sınırını aşıyor.");
            var key = new ArticleNoteKey(reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4));
            key.Validate();
            if (key.LawNumber.StartsWith("__EMBASIC_", StringComparison.Ordinal) ||
                !identities.Add(key))
                throw new InvalidDataException("Eski notların meta anahtarı veya tekrarlanan kimliği var.");
            string value = reader.GetString(5);
            long bytes = Encoding.UTF8.GetByteCount(value);
            total += bytes;
            if (string.IsNullOrWhiteSpace(value) || bytes > 50_000 || total > MaxTotalTextBytes)
                throw new InvalidDataException("Eski not içerik sınırı aşıldı.");
            var note = new ArticleNote(reader.GetString(0), key, value,
                DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture));
            if (string.IsNullOrWhiteSpace(note.Id) || note.Id.Length > 128)
                throw new InvalidDataException("Eski not kimliği bozuk.");
            notes.Add(note);
        }
        return notes;
    }
}
