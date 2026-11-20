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
    private readonly string _databasePath;
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
        _databasePath = fullPath;
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
        if (string.IsNullOrWhiteSpace(text) || Encoding.UTF8.GetByteCount(text) > 50_000)
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

    /// <summary>
    /// All-or-nothing, no overwrite. Only explicit legacy migration calls this method.
    /// </summary>
    public void ImportNewNotesAtomically(IReadOnlyList<ArticleNote> legacyNotes)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(legacyNotes);
        if (legacyNotes.Count is < 1 or > 1000)
            throw new ArgumentException("Not aktarımı 1 ile 1000 kayıt arasında olmalıdır.");
        var keys = new HashSet<ArticleNoteKey>();
        var encrypted = new List<ArticleNote>(legacyNotes.Count);
        foreach (var old in legacyNotes)
        {
            ValidateUserKey(old.Key);
            if (!keys.Add(old.Key))
                throw new InvalidDataException("Tekrarlanan madde ve sürüm kimliği.");
            if (string.IsNullOrWhiteSpace(old.Text) ||
                Encoding.UTF8.GetByteCount(old.Text) > 50_000)
                throw new InvalidDataException("Aktarım içeriği boş veya 50 KB üstünde.");
            if (HasNote(old.Key))
                throw new InvalidOperationException("Hedef kasada aynı kimlikte not var; üzerine yazma yasak.");
            encrypted.Add(old with { Text = Encrypt(old.Text, old.Key, _encryptionKey) });
        }
        // Each INSERT runs under one transaction, with zero upsert/overwrite behavior.
        _store.InsertUnchangedBatch(encrypted);
    }

    public bool Delete(ArticleNoteKey key)
    {
        EnsureOpen();
        ValidateUserKey(key);
        return _store.Delete(key);
    }

    public bool CheckIntegrity()
    {
        EnsureOpen();
        return AuthenticateStore(_store);
    }

    private bool AuthenticateStore(ArticleNoteStore store)
    {
        try
        {
            if (!store.CheckIntegrity()) return false;
            int saltCount = 0, sentinelCount = 0;
            store.InspectAllRows(row =>
            {
                if (row.Key == SaltKey)
                {
                    if (++saltCount != 1 ||
                        !string.Equals(row.Text, Convert.ToBase64String(_salt),
                            StringComparison.Ordinal))
                        throw new InvalidDataException("Kasa salt üstverisi değişmiş.");
                }
                else if (row.Key == CheckKey)
                {
                    if (++sentinelCount != 1 ||
                        !string.Equals(Decrypt(row.Text, CheckKey, _encryptionKey),
                            Sentinel, StringComparison.Ordinal))
                        throw new CryptographicException("Kasa doğrulama sentinel hatası.");
                }
                else
                {
                    ValidateUserKey(row.Key);
                    string value = Decrypt(row.Text, row.Key, _encryptionKey);
                    if (string.IsNullOrWhiteSpace(value) ||
                        Encoding.UTF8.GetByteCount(value) > 50_000)
                        throw new InvalidDataException("Kasa şifreli notunun içerik sınırı geçersiz.");
                }
            });
            return saltCount == 1 && sentinelCount == 1;
        }
        catch (Exception ex) when (ex is CryptographicException or InvalidDataException
            or FormatException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Refuse to create or overwrite a backup when any encrypted note fails
    /// authentication. After the SQLite online backup, independently open and
    /// authenticate the snapshot before reporting success. Preserve the prior
    /// backup on failed source verification.
    /// </summary>
    public string BackupTo(string path)
    {
        EnsureOpen();
        if (!CheckIntegrity())
            throw new InvalidDataException("Kasa şifreli içerik doğrulaması başarısız; yedek reddedildi.");
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Yedek yolu gerekli.", nameof(path));
        string destination = Path.GetFullPath(path);
        if (string.Equals(destination, _databasePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Kasa dosyası üzerine yedek alınamaz.", nameof(path));
        // The existing complete backup MUST survive a corrupted SQLite/AES
        // snapshot. The actual destination is replaced only after an entirely
        // authenticated staged snapshot has passed validation.
        string staging = destination + "." + Guid.NewGuid().ToString("N") + ".auth.tmp";
        try
        {
            _store.BackupTo(staging);
            using (var snapshotStore = new ArticleNoteStore(staging))
                if (!AuthenticateStore(snapshotStore))
                    throw new InvalidDataException("Yedek şifreli içerik doğrulaması başarısız.");
            File.Move(staging, destination, overwrite: true);
            return destination;
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    /// <summary>
    /// Read-only, content-free recovery assessment. Differences are computed
    /// from authenticated encrypted rows, not from easily altered timestamps.
    /// A snapshot fingerprint is NOT an anti-rollback trusted counter.
    /// </summary>
    public sealed record RestorePreview(
        string BackupSha256,
        string? TargetSha256,
        int BackupNotes,
        int TargetNotes,
        int ChangedNotes,
        int MissingFromBackup)
    {
        public bool WouldDiscardCurrentData => ChangedNotes > 0 || MissingFromBackup > 0;
    }

    private Dictionary<ArticleNoteKey, (string Id, string CipherHash)> AuthenticatedFingerprints()
    {
        if (!CheckIntegrity())
            throw new InvalidDataException("Şifreli kasa içeriği doğrulanmadı; geri yükleme durduruldu.");
        var output = new Dictionary<ArticleNoteKey, (string Id, string CipherHash)>();
        _store.InspectAllRows(row =>
        {
            if (row.Key.LawNumber.StartsWith("__EMBASIC_", StringComparison.Ordinal))
                return;
            string cipherHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(row.Text)));
            if (!output.TryAdd(row.Key, (row.Id, cipherHash)))
                throw new InvalidDataException("Tekrarlanan not kimliği.");
        });
        return output;
    }

    private static string FileSha256(string path)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }

    /// <summary>
    /// Does not change either file. Compares every authenticated note identity
    /// and ciphertext in backup vs target. Modified or missing target notes
    /// require a separate explicit confirmation before actual replacement.
    /// </summary>
    public static RestorePreview PreviewRestore(
        string sourceBackup, string destination, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(sourceBackup) || string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Yedek ve hedef yolu zorunludur.");
        string backup = Path.GetFullPath(sourceBackup);
        string target = Path.GetFullPath(destination);
        if (string.Equals(backup, target, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Yedek ile hedef kasa aynı dosya olamaz.");
        if (!File.Exists(backup))
            throw new FileNotFoundException("Geri yükleme yedeği bulunamadı.", backup);

        string backupHashBefore = FileSha256(backup);
        using var source = new EncryptedArticleNotes(backup, passphrase);
        var fromBackup = source.AuthenticatedFingerprints();
        string backupHashAfter = FileSha256(backup);
        if (backupHashBefore != backupHashAfter)
            throw new IOException("Ön inceleme sırasında yedek değişti.");

        if (!File.Exists(target))
            return new RestorePreview(backupHashAfter, null,
                fromBackup.Count, 0, 0, 0);

        string targetHashBefore = FileSha256(target);
        using var existing = new EncryptedArticleNotes(target, passphrase);
        if (!existing._salt.AsSpan().SequenceEqual(source._salt))
            throw new InvalidDataException("Yedek başka bir şifreli kasaya aittir.");
        var current = existing.AuthenticatedFingerprints();
        string targetHashAfter = FileSha256(target);
        if (targetHashBefore != targetHashAfter)
            throw new IOException("Ön inceleme sırasında mevcut kasa değişti.");
        int missing = 0, changed = 0;
        foreach (var (key, value) in current)
        {
            if (!fromBackup.TryGetValue(key, out var saved))
                missing++;
            else if (saved.Id != value.Id ||
                     !string.Equals(saved.CipherHash, value.CipherHash, StringComparison.Ordinal))
                changed++;
        }
        return new RestorePreview(backupHashAfter, targetHashAfter,
            fromBackup.Count, current.Count, changed, missing);
    }

    /// <summary>
    /// Validate BOTH vaults with the supplied passphrase before replacing the target.
    /// No source-to-destination copy while the target is opened by this application.
    /// Keeps pre-restore rollback on success. No automatic plaintext migration.
    /// </summary>
    public static string RestoreFrom(string sourceBackup, string destination, string passphrase,
        bool userConfirmedDataLoss = false, RestorePreview? approvedPreview = null)
    {
        if (string.IsNullOrWhiteSpace(sourceBackup) || string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Yedek ve hedef yolu zorunludur.");
        string backup = Path.GetFullPath(sourceBackup);
        string target = Path.GetFullPath(destination);
        if (string.Equals(backup, target, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Yedek hedef dosyanın kendisi olamaz.");
        if (!File.Exists(backup)) throw new FileNotFoundException("Yedek bulunamadı.", backup);
        RestorePreview preview = PreviewRestore(backup, target, passphrase);
        if (approvedPreview is not null && approvedPreview != preview)
            throw new IOException("Onaydan sonra yedek veya mevcut kasa değişti; yeniden ön inceleme gerekli.");
        if (preview.WouldDiscardCurrentData && !userConfirmedDataLoss)
            throw new InvalidOperationException(
                "Eski yedek mevcut notları değiştirecek veya silecek; açık kullanıcı onayı olmadan geri yükleme reddedildi.");
        byte[] originalSalt;
        using (var old = new EncryptedArticleNotes(backup, passphrase))
        {
            if (!old.CheckIntegrity())
                throw new InvalidDataException("Yedek şifreli notlardan biri veya SQLite yapısı bozuk; geri yükleme reddedildi.");
            originalSalt = (byte[])old._salt.Clone();
        }

        // Prevent accidental replacement of another independently created vault.
        if (File.Exists(target))
        {
            using var present = new EncryptedArticleNotes(target, passphrase);
            if (!present.CheckIntegrity())
                throw new InvalidDataException("Mevcut kasada bozuk şifreli not var; geri yüklemeden önce insan incelemesi gerekli.");
            if (!present._salt.AsSpan().SequenceEqual(originalSalt))
                throw new InvalidDataException("Yedek başka bir kasaya aittir; üzerine yazma reddedildi.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string staged = target + "." + Guid.NewGuid().ToString("N") + ".restore.tmp";
        try
        {
            if (FileSha256(backup) != preview.BackupSha256)
                throw new IOException("Yedek doğrulama ile kopyalama arasında değişti.");
            File.Copy(backup, staged);
            if (FileSha256(staged) != preview.BackupSha256)
                throw new IOException("Geçici kopya SHA-256 doğrulamasında uyuşmazlık.");
            using (var verify = new EncryptedArticleNotes(staged, passphrase))
            {
                if (!verify._salt.AsSpan().SequenceEqual(originalSalt) || !verify.CheckIntegrity())
                    throw new InvalidDataException("Geri yükleme kopyası doğrulanamadı.");
            }

            if (preview.TargetSha256 is not null)
            {
                if (!File.Exists(target) || FileSha256(target) != preview.TargetSha256)
                    throw new IOException("Hedef kasa onaydan sonra değişti; geri yükleme reddedildi.");
                string rollback = target + ".pre-restore.sqlite3";
                // If rollback exists, refuse overwriting the only prior recovery point.
                if (File.Exists(rollback))
                    throw new IOException("Önceki geri alma yedeği mevcut; ezilmeyecek.");
                File.Replace(staged, target, rollback);
            }
            else
            {
                if (File.Exists(target))
                    throw new IOException("Ön inceleme sonrası yeni hedef kasa oluştu; üzerine yazılmadı.");
                File.Move(staged, target);
            }
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
