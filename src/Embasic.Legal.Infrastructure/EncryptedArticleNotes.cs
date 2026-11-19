using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Embasic.Legal.Infrastructure;

/// <summary>
/// Password-derived AES-256-GCM content encryption backed by the existing SQLite store.
/// Law/article/version identifiers remain plaintext. No legacy plaintext migration occurs.
/// </summary>
public sealed class EncryptedArticleNotes : IDisposable
{
    private const string EnvelopePrefix = "EAL4:";
    private const int Iterations = 310_000;
    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const string Sentinel = "EMBASIC-LEGAL-V4-VERIFIED-VAULT";
    private static readonly ArticleNoteKey SaltKey =
        new("__EMBASIC_V4_METADATA__", "KDF-SALT", "", "1");
    private static readonly ArticleNoteKey CheckKey =
        new("__EMBASIC_V4_METADATA__", "PASSWORD-CHECK", "", "1");
    private readonly ArticleNoteStore _store;
    private readonly byte[] _encryptionKey;
    private readonly byte[] _salt;
    private bool _disposed;

    public static string DefaultDatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Embasic", "Legal", "article-notes-v4-encrypted.sqlite3");

    public EncryptedArticleNotes(string databasePath, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Kasa yolu gerekli.", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(passphrase) || passphrase.Length < 12)
            throw new ArgumentException("Kasa parolası en az 12 karakter olmalıdır.", nameof(passphrase));

        string fullPath = Path.GetFullPath(databasePath);
        bool existing = File.Exists(fullPath);
        _store = new ArticleNoteStore(fullPath);
        byte[]? derived = null;
        try
        {
            if (existing)
            {
                string saltText = _store.Get(SaltKey)?.Text ??
                    throw new InvalidDataException("Kasa başlığı yok. Eski düz metin dosyası otomatik dönüştürülmez.");
                string verifier = _store.Get(CheckKey)?.Text ??
                    throw new InvalidDataException("Kasa doğrulama başlığı bulunamadı.");
                _salt = Convert.FromBase64String(saltText);
                if (_salt.Length != SaltLength)
                    throw new InvalidDataException("Kasa salt boyutu geçersiz.");
                derived = Rfc2898DeriveBytes.Pbkdf2(passphrase, _salt, Iterations,
                    HashAlgorithmName.SHA256, 32);
                string plaintext = Decrypt(verifier, CheckKey, derived);
                if (!string.Equals(plaintext, Sentinel, StringComparison.Ordinal))
                    throw new CryptographicException("Parola doğrulaması başarısız.");
            }
            else
            {
                _salt = RandomNumberGenerator.GetBytes(SaltLength);
                derived = Rfc2898DeriveBytes.Pbkdf2(passphrase, _salt, Iterations,
                    HashAlgorithmName.SHA256, 32);
                _store.Save(SaltKey, Convert.ToBase64String(_salt));
                _store.Save(CheckKey, Encrypt(Sentinel, CheckKey, derived));
            }

            _encryptionKey = derived;
            derived = null;
            if (!_store.CheckIntegrity())
                throw new InvalidDataException("SQLite kasa bütünlüğü başarısız.");
        }
        catch
        {
            if (derived is not null) CryptographicOperations.ZeroMemory(derived);
            _store.Dispose();
            throw;
        }
    }

    public ArticleNote Save(ArticleNoteKey key, string text)
    {
        EnsureOpen();
        ValidateUserKey(key);
        if (string.IsNullOrWhiteSpace(text) || text.Length > 100_000)
            throw new ArgumentException("Not boş veya çok uzun.", nameof(text));
        string cipher = Encrypt(text, key, _encryptionKey);
        return _store.Save(key, cipher) with { Text = text };
    }

    public ArticleNote? Get(ArticleNoteKey key)
    {
        EnsureOpen();
        ValidateUserKey(key);
        var note = _store.Get(key);
        return note is null ? null : note with
        {
            Text = Decrypt(note.Text, key, _encryptionKey)
        };
    }

    public bool HasNote(ArticleNoteKey key) => Get(key) is not null;

    public bool Delete(ArticleNoteKey key)
    {
        EnsureOpen();
        ValidateUserKey(key);
        return _store.Delete(key);
    }

    public bool CheckIntegrity()
    {
        EnsureOpen();
        return _store.CheckIntegrity();
    }

    public string BackupTo(string path)
    {
        EnsureOpen();
        return _store.BackupTo(path);
    }

    /// <summary>
    /// Validate BOTH vaults with the supplied passphrase before replacing the target.
    /// No source-to-destination copy while the target is opened by this application.
    /// Keeps pre-restore rollback on success. No automatic plaintext migration.
    /// </summary>
    public static string RestoreFrom(string sourceBackup, string destination, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(sourceBackup) || string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Yedek ve hedef yolu zorunludur.");
        string backup = Path.GetFullPath(sourceBackup);
        string target = Path.GetFullPath(destination);
        if (string.Equals(backup, target, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Yedek hedef dosyanın kendisi olamaz.");
        if (!File.Exists(backup)) throw new FileNotFoundException("Yedek bulunamadı.", backup);
        byte[] originalSalt;
        using (var old = new EncryptedArticleNotes(backup, passphrase))
        {
            if (!old.CheckIntegrity()) throw new InvalidDataException("Yedek SQLite bütünlüğü geçersiz.");
            originalSalt = (byte[])old._salt.Clone();
        }

        // Prevent accidental replacement of another independently created vault.
        if (File.Exists(target))
        {
            using var present = new EncryptedArticleNotes(target, passphrase);
            if (!present._salt.AsSpan().SequenceEqual(originalSalt))
                throw new InvalidDataException("Yedek başka bir kasaya aittir; üzerine yazma reddedildi.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string staged = target + "." + Guid.NewGuid().ToString("N") + ".restore.tmp";
        try
        {
            File.Copy(backup, staged);
            using (var verify = new EncryptedArticleNotes(staged, passphrase))
            {
                if (!verify._salt.AsSpan().SequenceEqual(originalSalt) || !verify.CheckIntegrity())
                    throw new InvalidDataException("Geri yükleme kopyası doğrulanamadı.");
            }

            if (File.Exists(target))
            {
                string rollback = target + ".pre-restore.sqlite3";
                // If rollback exists, refuse overwriting the only prior recovery point.
                if (File.Exists(rollback))
                    throw new IOException("Önceki geri alma yedeği mevcut; ezilmeyecek.");
                File.Replace(staged, target, rollback);
            }
            else File.Move(staged, target);
            return target;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(originalSalt);
            if (File.Exists(staged)) File.Delete(staged);
        }
    }

    private static void ValidateUserKey(ArticleNoteKey key)
    {
        key.Validate();
        if (key.LawNumber.StartsWith("__EMBASIC_", StringComparison.Ordinal))
            throw new ArgumentException("Sistem meta anahtarları not için kullanılamaz.");
    }

    private static byte[] AAD(ArticleNoteKey key) =>
        JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            key.LawNumber, key.ArticleKey, key.SubdivisionKey, key.VersionKey
        });

    private static string Encrypt(string text, ArticleNoteKey key, byte[] encryptionKey)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceLength);
        byte[] clear = Encoding.UTF8.GetBytes(text);
        byte[] ciphertext = new byte[clear.Length];
        byte[] tag = new byte[TagLength];
        try
        {
            using var aes = new AesGcm(encryptionKey, TagLength);
            aes.Encrypt(nonce, clear, ciphertext, tag, AAD(key));
            byte[] combined = new byte[NonceLength + TagLength + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, NonceLength);
            Buffer.BlockCopy(tag, 0, combined, NonceLength, TagLength);
            Buffer.BlockCopy(ciphertext, 0, combined, NonceLength + TagLength, ciphertext.Length);
            return EnvelopePrefix + Convert.ToBase64String(combined);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    private static string Decrypt(string envelope, ArticleNoteKey key, byte[] encryptionKey)
    {
        if (!envelope.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
            throw new CryptographicException("Düz metin veya tanınmayan kasa verisi reddedildi.");
        byte[] combined = Convert.FromBase64String(envelope[EnvelopePrefix.Length..]);
        if (combined.Length < NonceLength + TagLength)
            throw new CryptographicException("Şifreli içerik eksik.");
        byte[] clear = new byte[combined.Length - NonceLength - TagLength];
        try
        {
            using var aes = new AesGcm(encryptionKey, TagLength);
            aes.Decrypt(combined.AsSpan(0, NonceLength),
                combined.AsSpan(NonceLength + TagLength),
                combined.AsSpan(NonceLength, TagLength), clear, AAD(key));
            return Encoding.UTF8.GetString(clear);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    private void EnsureOpen()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EncryptedArticleNotes));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _store.Dispose();
        CryptographicOperations.ZeroMemory(_encryptionKey);
        CryptographicOperations.ZeroMemory(_salt);
    }
}
