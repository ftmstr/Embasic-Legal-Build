using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Embasic.Legal.Infrastructure;

/// <summary>Identity of a user note. The immutable version key prevents silent transfer to another legal text.</summary>
public sealed record ArticleNoteKey(string LawNumber, string ArticleKey, string SubdivisionKey, string VersionKey)
{
    public void Validate()
    {
        foreach (var value in new[] { LawNumber, ArticleKey, VersionKey })
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
                throw new ArgumentException("Kanun, madde ve sürüm kimliği boş olamaz veya 128 karakteri aşamaz.");
        if (SubdivisionKey is null || SubdivisionKey.Length > 128)
            throw new ArgumentException("Alt bölüm kimliği geçersiz.");
    }
}
public sealed record ArticleNote(string Id, ArticleNoteKey Key, string Text, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);

/// <summary>Local, parameterized SQLite store. Not encrypted; do not store case secrets before security acceptance.</summary>
public sealed class ArticleNoteStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _databasePath;

    public ArticleNoteStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Veritabanı yolu gerekli.", nameof(databasePath));
        var fullPath = Path.GetFullPath(databasePath);
        _databasePath = fullPath;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false // Release Windows file handles immediately after Dispose; safe backup/test cleanup.
        };
        _connection = new SqliteConnection(builder.ToString());
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ArticleNotes (
                Id TEXT NOT NULL PRIMARY KEY,
                LawNumber TEXT NOT NULL,
                ArticleKey TEXT NOT NULL,
                SubdivisionKey TEXT NOT NULL,
                VersionKey TEXT NOT NULL,
                NoteText TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                UNIQUE (LawNumber, ArticleKey, SubdivisionKey, VersionKey)
            );
            CREATE INDEX IF NOT EXISTS IX_ArticleNotes_LawArticle
                ON ArticleNotes(LawNumber, ArticleKey);
            """;
        cmd.ExecuteNonQuery();
        using var durability = _connection.CreateCommand();
        durability.CommandText = "PRAGMA synchronous=FULL;";
        durability.ExecuteNonQuery();
    }

    public static string DefaultDatabasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Embasic", "Legal", "article-notes.sqlite3");

    public ArticleNote Save(ArticleNoteKey key, string text)
    {
        key.Validate();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 100_000)
            throw new ArgumentException("Not boş olamaz ve 100.000 karakteri aşamaz.", nameof(text));
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ArticleNotes(Id, LawNumber, ArticleKey, SubdivisionKey,
                                     VersionKey, NoteText, CreatedUtc, UpdatedUtc)
            VALUES ($id, $law, $article, $subdivision, $version, $text, $now, $now)
            ON CONFLICT(LawNumber, ArticleKey, SubdivisionKey, VersionKey)
            DO UPDATE SET NoteText=excluded.NoteText, UpdatedUtc=excluded.UpdatedUtc;
            """;
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        BindIdentity(cmd, key);
        cmd.Parameters.AddWithValue("$text", text);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.ExecuteNonQuery();
        return Get(key) ?? throw new InvalidOperationException("Kaydedilen not geri okunamadı.");
    }

    public ArticleNote? Get(ArticleNoteKey key)
    {
        key.Validate();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, NoteText, CreatedUtc, UpdatedUtc
            FROM ArticleNotes
            WHERE LawNumber=$law AND ArticleKey=$article
              AND SubdivisionKey=$subdivision AND VersionKey=$version
            LIMIT 1;
            """;
        BindIdentity(cmd, key);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new ArticleNote(reader.GetString(0), key, reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture));
    }

    public bool Delete(ArticleNoteKey key)
    {
        key.Validate();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM ArticleNotes
            WHERE LawNumber=$law AND ArticleKey=$article
              AND SubdivisionKey=$subdivision AND VersionKey=$version;
            """;
        BindIdentity(cmd, key);
        return cmd.ExecuteNonQuery() == 1;
    }

    public bool HasAnyNote(string lawNumber, string articleKey)
    {
        if (string.IsNullOrWhiteSpace(lawNumber) || string.IsNullOrWhiteSpace(articleKey))
            throw new ArgumentException("Kanun ve madde kimliği gerekli.");
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT EXISTS(SELECT 1 FROM ArticleNotes
                          WHERE LawNumber=$law AND ArticleKey=$article);
            """;
        cmd.Parameters.AddWithValue("$law", lawNumber);
        cmd.Parameters.AddWithValue("$article", articleKey);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    private static void BindIdentity(SqliteCommand cmd, ArticleNoteKey key)
    {
        cmd.Parameters.AddWithValue("$law", key.LawNumber);
        cmd.Parameters.AddWithValue("$article", key.ArticleKey);
        cmd.Parameters.AddWithValue("$subdivision", key.SubdivisionKey);
        cmd.Parameters.AddWithValue("$version", key.VersionKey);
    }

    /// <summary>SQLite online snapshot, verified before atomic replacement of an earlier backup.</summary>
    public string BackupTo(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Yedek yolu boş olamaz.", nameof(backupPath));
        string fullPath = Path.GetFullPath(backupPath);
        if (string.Equals(fullPath, _databasePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Yedek kaynak veritabanının üzerine yazılamaz.", nameof(backupPath));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = temporary,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };
            using (var backup = new SqliteConnection(builder.ToString()))
            {
                backup.Open();
                _connection.BackupDatabase(backup);
                using var integrity = backup.CreateCommand();
                integrity.CommandText = "PRAGMA quick_check;";
                if (!string.Equals(Convert.ToString(integrity.ExecuteScalar(),
                        CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("SQLite yedeği bütünlük kontrolünü geçemedi.");
            }

            // The previous complete backup survives failed temporary writes.
            File.Move(temporary, fullPath, overwrite: true);
            return fullPath;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public bool CheckIntegrity()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA quick_check;";
        return string.Equals(Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture),
            "ok", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Insert a complete batch without upserts. Any pre-existing key or ID fails
    /// the whole transaction; timestamps and original note IDs are preserved.
    /// Only intended for explicit, previewed migration into an authenticated vault.
    /// </summary>
    public void InsertUnchangedBatch(IReadOnlyList<ArticleNote> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        if (notes.Count == 0) throw new ArgumentException("Boş not aktarımı yapılamaz.");
        using var transaction = _connection.BeginTransaction();
        foreach (var note in notes)
        {
            note.Key.Validate();
            if (string.IsNullOrWhiteSpace(note.Id) || note.Id.Length > 128 ||
                string.IsNullOrWhiteSpace(note.Text) || note.Text.Length > 100_000)
                throw new InvalidDataException("Aktarım notunun ID veya içeriği geçersiz.");
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO ArticleNotes (Id, LawNumber, ArticleKey, SubdivisionKey,
                    VersionKey, NoteText, CreatedUtc, UpdatedUtc)
                VALUES ($id, $law, $article, $subdivision, $version, $text, $created, $updated);
                """;
            cmd.Parameters.AddWithValue("$id", note.Id);
            BindIdentity(cmd, note.Key);
            cmd.Parameters.AddWithValue("$text", note.Text);
            cmd.Parameters.AddWithValue("$created", note.CreatedUtc.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$updated", note.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            if (cmd.ExecuteNonQuery() != 1)
                throw new IOException("Not aktarımında beklenmeyen kayıt sayısı.");
        }
        transaction.Commit();
    }

    public void Dispose() => _connection.Dispose();
}
