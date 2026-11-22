using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Embasic.Legal.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace Embasic.Legal.Desktop.ViewModels;

/// <summary>
/// Synthetic, explicitly non-official note demo. New plaintext is never stored in the old TUR003 database.
/// Passwords are taken directly from PasswordBox, never stored on a ViewModel property.
/// </summary>
public sealed class NoteSandboxViewModel : ObservableViewModel
{
    private readonly ArticleNoteKey _key =
        new("SANDBOX", "SYNTHETIC-ARTICLE", "", "TUR003-EXPLICIT-DEMO");
    private readonly DispatcherTimer _debounce;
    private EncryptedArticleNotes? _store;
    // TUR019 AUTO_SQLITE_METADATA_CONSENT — one unlocked session only.
    private readonly RawMetadataAutoBackupConsent _automaticBackupConsent = new();
    private LegacyMigrationPreview? _migrationPreview;
    private bool _migrationConsent;
    private string _migrationStatus =
        "Eski düz metin notları otomatik aktarılmaz. Şifreli kasa açıldıktan sonra ön inceleme yapın.";
    private bool _dirty;
    private bool _hasNote;
    private bool _isOpen;
    private bool _expanded;
    private string _draft = "";
    private string _status =
        "Şifreli kasa kilitli. 12+ karakter parola girin; eski şifresiz notlar ayrı korunur.";

    public NoteSandboxViewModel()
    {
        ToggleCommand = new RelayCommand(_ => Toggle());
        ExpandCommand = new RelayCommand(_ => Expand());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete());
        CloseCommand = new RelayCommand(_ => Close());
        UnlockCommand = new RelayCommand(parameter => Unlock(parameter as PasswordBox));
        RestoreCommand = new RelayCommand(parameter => Restore(parameter as PasswordBox));
        LockCommand = new RelayCommand(_ => Lock());
        ExportBackupCommand = new RelayCommand(_ => ExportBackup());
        ExportWitnessCommand = new RelayCommand(_ => ExportWitness());
        VerifyWitnessCommand = new RelayCommand(_ => VerifyWitness());
        PreviewLegacyCommand = new RelayCommand(_ => PreviewLegacy());
        AuditLegacyExposureCommand = new RelayCommand(_ => AuditLegacyExposure());
        ApplyLegacyCommand = new RelayCommand(_ => ApplyLegacy());

        _debounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(900)
        };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Save(); };
    }

    public string Draft
    {
        get => _draft;
        set
        {
            if (value == _draft) return;
            _draft = value;
            _dirty = true;
            Changed();
            _debounce.Stop();
            if (!string.IsNullOrWhiteSpace(value) && _store is not null)
            {
                Status = "Değişiklik bekliyor; 900 ms sonra şifreli kayıt deneniyor.";
                _debounce.Start();
            }
            else Status = "Boş not kaydedilmez; silmek için Sil düğmesini kullanın.";
        }
    }

    public bool HasNote { get => _hasNote; private set { _hasNote = value; Changed(); } }
    public bool IsOpen { get => _isOpen; set { _isOpen = value; Changed(); } }
    public double EditorWidth => _expanded ? 610 : 355;
    public double EditorHeight => _expanded ? 475 : 240;
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public ICommand ToggleCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand UnlockCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand LockCommand { get; }
    public ICommand ExportBackupCommand { get; }
    public ICommand ExportWitnessCommand { get; }
    public ICommand VerifyWitnessCommand { get; }
    public ICommand PreviewLegacyCommand { get; }
    public ICommand AuditLegacyExposureCommand { get; }
    private string _legacyExposureStatus =
        "Düz metin kaynak envanteri yalnız bilinen yerel klasöre bakar; içerik veya dosya adı dışa aktarılmaz.";
    public string LegacyExposureStatus
    {
        get => _legacyExposureStatus;
        private set { _legacyExposureStatus = value; Changed(); }
    }

    public ICommand ApplyLegacyCommand { get; }
    public bool MigrationConsent
    {
        get => _migrationConsent;
        set { _migrationConsent = value; Changed(); }
    }
    public string MigrationStatus
    {
        get => _migrationStatus;
        private set { _migrationStatus = value; Changed(); }
    }

    private void Unlock(PasswordBox? input)
    {
        if (input is null) { Status = "Parola alanı bulunamadı."; return; }
        string pass = input.Password;
        input.Clear();
        if (_store is not null) { Status = "Kasa zaten açık."; return; }
        try
        {
            var candidate = new EncryptedArticleNotes(EncryptedArticleNotes.DefaultDatabasePath, pass);
            try
            {
                var saved = candidate.Get(_key);
                _store = candidate;
                _automaticBackupConsent.Reset();
                _draft = saved?.Text ?? "";
                _dirty = false;
                Changed(nameof(Draft));
                HasNote = saved is not null;
                Status = "Kasa açıldı. Yalnız sentetik örnek notu düzenlenebilir.";
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or CryptographicException or ArgumentException or FormatException)
        {
            Status = "Kasa açılmadı; mevcut veri korunuyor: " + ex.Message;
        }
    }

    private void Restore(PasswordBox? input)
    {
        if (input is null) { Status = "Parola alanı bulunamadı."; return; }
        string pass = input.Password;
        input.Clear();
        if (_store is not null)
        {
            Status = "Geri yükleme için önce kasayı kilitleyin. Açık DB üzerine yazılamaz.";
            return;
        }
        var dialog = new OpenFileDialog
        {
            Title = "Aynı şifreli kasanın doğrulanmış SQLite yedeği",
            Filter = "SQLite yedeği (*.sqlite3;*.db;*.backup)|*.sqlite3;*.db;*.backup|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var preview = EncryptedArticleNotes.PreviewRestore(
                dialog.FileName, EncryptedArticleNotes.DefaultDatabasePath, pass);
            bool destructive = preview.WouldDiscardCurrentData;
            string warning = destructive
                ? $"DİKKAT: Bu yedek mevcut kasadaki {preview.ChangedNotes} notun içeriğini değiştirecek, " +
                  $"{preview.MissingFromBackup} yeni not yedekte olmadığından kaybolacak. " +
                  "Geri alma dosyası korunacak. Bu veri kaybını AÇIKÇA onaylıyor musunuz?"
                : $"Yedekte {preview.BackupNotes} not; mevcut kasada {preview.TargetNotes} not var. " +
                  "Değişmiş veya yedekte eksik hedef not saptanmadı. Geri yüklensin mi?";
            if (MessageBox.Show(warning,
                    destructive ? "EMBASIC — AÇIK VERİ KAYBI ONAYI" : "EMBASIC — Yedek Ön İnceleme",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                Status = "Geri yükleme reddedildi; mevcut kasa korunuyor.";
                return;
            }
            EncryptedArticleNotes.RestoreFrom(
                dialog.FileName, EncryptedArticleNotes.DefaultDatabasePath, pass,
                userConfirmedDataLoss: destructive, approvedPreview: preview);
            Status = destructive
                ? "Kullanıcı onaylı geri dönüş tamamlandı; önceki kasa .pre-restore.sqlite3 olarak korundu."
                : "Yedek başarıyla doğrulanıp geri yüklendi.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or CryptographicException or ArgumentException or FormatException
            or InvalidOperationException)
        {
            Status = "Geri yükleme reddedildi. Mevcut kasa korundu: " + ex.Message;
        }
    }

    private void ExportBackup()
    {
        if (_store is null) { Status = "Önce kasayı açın."; return; }
        if (!FlushPending()) return;
        var dialog = new SaveFileDialog
        {
            Title = "Şifreli SQLite yedeğini kaydet",
            FileName = "embasic-notes-encrypted.sqlite3",
            DefaultExt = ".sqlite3",
            Filter = "SQLite yedeği (*.sqlite3)|*.sqlite3",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true) return;
        // AES-GCM protects note bodies, NOT SQLite law/article/version identifiers,
        // timestamps or the number of records in the exported raw database.
        if (MessageBox.Show(
                "Bu yedek şifreli not içerikleri taşır; ancak kanun/madde/sürüm kimlikleri, tarih ve SQLite üstverisi ŞİFRESİZDİR. " +
                "Şifrelenmiş arşiv değildir. Haricî konuma kaydetmenin gizlilik riskini kabul ederek devam edilsin mi?",
                "EMBASIC — ŞİFRESİZ ÜSTVERİ DIŞA AKTARIMI",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            Status = "Yedek dışa aktarımı metadata gizliliği nedeniyle iptal edildi.";
            return;
        }
        try
        {
            _store.BackupTo(dialog.FileName);
            Status = "Şifreli not yedeği oluşturuldu. Parola ve üstveri hassasiyetini unutmayın.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            Status = "Yedek dışa aktarılamadı: " + ex.Message;
        }
    }


    /// <summary>
    /// User-selected standalone witness. This is not stored inside the vault
    /// and is not silently refreshed on future edits. No note plaintext leaves.
    /// </summary>
    private void ExportWitness()
    {
        if (_store is null) { Status = "Bütünlük makbuzu için önce kasayı açın."; return; }
        if (!FlushPending()) return;
        var dialog = new SaveFileDialog
        {
            Title = "Bağımsız saklanacak şifreli kasa bütünlük makbuzu",
            FileName = "embasic-vault-independent-witness.json",
            DefaultExt = ".json",
            Filter = "Bütünlük makbuzu (*.json)|*.json",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var receipt = _store.ExportIntegrityWitness(
                dialog.FileName, userConfirmedOverwrite: true);
            Status = $"Bütünlük makbuzu oluşturuldu ({receipt.NoteCount} not). " +
                "Dosyayı kasa ve yedeğinden BAĞIMSIZ bir yerde saklayın. " +
                "Notları sonraki düzenlemede yeni ayrı makbuz gerekir. " +
                "Makbuz hukuki veya resmî tamlık onayı DEĞİLDİR.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidDataException or CryptographicException)
        {
            Status = "Makbuz oluşturulamadı. Kasa değiştirilmedi: " + ex.Message;
        }
    }

    private void VerifyWitness()
    {
        if (_store is null) { Status = "Makbuz doğrulamak için önce kasayı açın."; return; }
        if (!FlushPending()) return;
        var dialog = new OpenFileDialog
        {
            Title = "Kasadan ayrı saklanan önceki bütünlük makbuzunu seç",
            Filter = "Bütünlük makbuzu (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            bool matching = _store.VerifyIntegrityWitness(dialog.FileName);
            Status = matching
                ? "Makbuz eşleşiyor: mevcut notların şifreli verileri ve metadata bu bağımsız kayıtla uyumlu. Resmî veya tam anti-rollback PASS DEĞİL."
                : "MAKBUZ UYUŞMUYOR: not silinmesi, güncelleme, metadata değişimi veya eski kasa ihtimali. Veri üzerine yazmayın; güvenli kopya alıp inceleyin.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidDataException or CryptographicException)
        {
            Status = "Makbuz doğrulanamadı. Hiçbir veri değiştirilmedi: " + ex.Message;
        }
    }

    private void AuditLegacyExposure()
    {
        // No password, open vault, or personal case data required. The scanner
        // never reads note text, changes ACL, deletes files, or walks a disk.
        try
        {
            var inventory = LegacyPlaintextExposureAudit.ScanKnownLocalDirectory();
            LegacyExposureStatus =
                $"Salt-okunur yerel risk envanteri: {inventory.KnownCandidates} bilinen isimli dosya, " +
                $"{inventory.PrimaryCandidates} eski ana kaynak, " +
                $"{inventory.SidecarCandidates} WAL/SHM/journal yan dosyası, " +
                $"{inventory.PotentialBackups} olası eski yedek. " +
                $"SQLite başlık izi: {inventory.SQLiteHeaderMatches}, " +
                $"aday toplam boyut: {inventory.TotalCandidateBytes} bayt, " +
                $"incelenemeyen girdi: {inventory.UnexaminedEntries}. " +
                (inventory.RequiresManualReview
                    ? "RİSK: Eski şifresiz kaynak/yan dosyalar olabilir; parola ve soruşturma verisi GİRMEYİN. Hiçbir dosya silinmedi."
                    : "Bilinen isimlerde aday yok. BU GÜVENLİK PASS DEĞİL; haricî, yeniden adlandırılmış veya başka klasördeki yedekler taranmadı.") +
                " Kaynak göçü otomatik değildir; şifreli kasanın SQLite metadata alanları hâlâ açıktır.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException)
        {
            LegacyExposureStatus =
                "Envanter tamamlanamadı (NOT_VERIFIED): " + ex.Message +
                " Hiçbir dosya değiştirilmedi.";
        }
    }

    private void PreviewLegacy()
    {
        MigrationConsent = false;
        _migrationPreview = null;
        if (_store is null)
        {
            MigrationStatus = "Ön inceleme için önce şifreli kasayı açın.";
            return;
        }
        if (!FlushPending())
        {
            MigrationStatus = "Kaydedilmemiş sentetik taslak var; ön inceleme yapılmadı.";
            return;
        }
        try
        {
            var preview = LegacyNoteMigration.Preview(ArticleNoteStore.DefaultDatabasePath);
            _migrationPreview = preview;
            MigrationStatus = $"Salt-okunur ön inceleme: {preview.NoteCount} not, " +
                $"{preview.TotalUtf8Bytes} UTF-8 bayt. SHA-256: {preview.SourceSha256}. " +
                "Henüz aktarım yapılmadı. Eski dosya/yedekler şifresiz kalacaktır.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or ArgumentException or FormatException)
        {
            MigrationStatus = "Ön inceleme reddedildi: " + ex.Message;
        }
    }

    private void ApplyLegacy()
    {
        if (_store is null || _migrationPreview is null || !MigrationConsent)
        {
            MigrationStatus = "Kasa açık, ön inceleme tamamlanmış ve açık onay kutusu işaretli olmalıdır.";
            return;
        }
        if (!FlushPending())
        {
            MigrationStatus = "Kaydedilmemiş taslak var; aktarım yapılmadı.";
            return;
        }
        var answer = MessageBox.Show(
            $"Eski şifresiz kaynaktaki {_migrationPreview.NoteCount} not şifreli kasaya aktarılacak. " +
            "Eski dosya ve yedekler ŞİFRESİZ olarak kalacak, otomatik silinmeyecek. " +
            "Mevcut notun üzerine yazılmayacak. Önce geri alma yedeği alınacak. " +
            "Bu işlemi açıkça onaylıyor musunuz?",
            "Embasic Legal — Onaylı Veri Aktarımı",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
        {
            MigrationConsent = false;
            MigrationStatus = "Kullanıcı onay vermedi; hiçbir not aktarılmadı.";
            return;
        }
        try
        {
            string backup = EncryptedArticleNotes.DefaultDatabasePath +
                ".pre-migration-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss") +
                "-" + Guid.NewGuid().ToString("N") + ".sqlite3";
            var result = LegacyNoteMigration.Apply(_migrationPreview, _store, true, backup);
            var sample = _store.Get(_key);
            _draft = sample?.Text ?? "";
            _dirty = false;
            Changed(nameof(Draft));
            HasNote = sample is not null;
            MigrationStatus = $"Şifreli aktarım tamamlandı: {result.ImportedNotes} not. " +
                $"Geri alma yedeği: {result.EncryptedPreMigrationBackup}. " +
                "Eski düz metin notlar ve yedekler silinmedi.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or ArgumentException or FormatException
            or InvalidOperationException or CryptographicException)
        {
            MigrationStatus = "Aktarım onaylanmadı veya başarısız: " + ex.Message +
                " Eski kaynak korunur. Varsa geri alma yedeğini saklayın.";
        }
        finally
        {
            MigrationConsent = false;
            _migrationPreview = null;
        }
    }

    private void Toggle()
    {
        if (IsOpen) Close();
        else if (_store is null) Status = "Kasa kilitli: önce parola ile açın.";
        else IsOpen = true;
    }

    private void Expand()
    {
        _expanded = !_expanded;
        Changed(nameof(EditorWidth));
        Changed(nameof(EditorHeight));
    }

    // Explicit consent is necessary even for automatic backups, because
    // AES-GCM does not encrypt law/article/version/timestamp SQLite columns.
    private bool AllowAutomaticSqliteBackup(bool forDelete)
    {
        if (_automaticBackupConsent.Approved) return true;
        if (_automaticBackupConsent.HasDecision && !forDelete) return false;
        bool agreed = MessageBox.Show(
            "OTOMATİK SQLITE YEDEĞİ: Not içerikleri şifrelidir; ancak kanun/madde/fıkra, " +
            "sürüm, zaman ve kayıt sayısı SQLite dosyasında ŞİFRESİZDİR. " +
            "Bu oturumda otomatik ham SQLite yedeği oluşturulmasına EVET diyor musunuz? " +
            "HAYIR: şifreli not kaydı devam eder fakat yedek ALINMAZ; " +
            "yedeksiz not silme reddedilir.",
            "EMBASIC — AÇIK ÜSTVERİ YEDEĞİ ONAYI",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        _automaticBackupConsent.Decide(agreed);
        return _automaticBackupConsent.Approved;
    }

    private bool Save()
    {
        _debounce.Stop();
        if (!_dirty) return true;
        if (string.IsNullOrWhiteSpace(Draft))
        {
            Status = "Boş not kaydedilmedi; önceki not silinmedi.";
            return false;
        }
        if (_store is null)
        {
            Status = "Kasa kapalı: taslak kaydedildi sayılmaz.";
            return false;
        }
        try
        {
            _store.Save(_key, Draft);
            _dirty = false;
            HasNote = true;
            if (!AllowAutomaticSqliteBackup(forDelete: false))
            {
                Status = "Şifreli not kaydedildi fakat açık üstveri onayı bulunmadığından " +
                    "OTOMATİK YEDEK ALINMADI. Kurtarma için ayrı güvenli yedek gerekir.";
            }
            else
            {
                try
                {
                    _automaticBackupConsent.BackupIfApproved(_store,
                        EncryptedArticleNotes.DefaultDatabasePath + ".backup.sqlite3");
                    Status = "Not kaydedildi; oturum onaylı ham SQLite yedeği alındı (üstveri açıktır).";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
                {
                    Status = "Şifreli not kaydedildi; ancak yedek başarısız: " + ex.Message;
                }
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or CryptographicException or ArgumentException)
        {
            Status = "Şifreli not kaydedilemedi; taslak açık: " + ex.Message;
            return false;
        }
    }

    private void Delete()
    {
        if (_store is null) { Status = "Kasa kilitli."; return; }
        if (MessageBox.Show("Sentetik şifreli not silinsin mi? Önce yedeklenecek.",
                "Embasic Legal — Deneme Notu", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;
        if (!AllowAutomaticSqliteBackup(forDelete: true))
        {
            Status = "Silme İPTAL: açık üstverili SQLite yedeği onaylanmadı; not korunuyor.";
            return;
        }
        try
        {
            _debounce.Stop();
            _automaticBackupConsent.RequireApprovalForDeletion();
            _automaticBackupConsent.BackupIfApproved(_store,
                EncryptedArticleNotes.DefaultDatabasePath + ".backup.sqlite3");
            _store.Delete(_key);
            _draft = "";
            Changed(nameof(Draft));
            _dirty = false;
            HasNote = false;
            Status = "Sentetik not silindi; eski şifreli kopya yedekte tutuldu.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            Status = "Silme gerçekleşmedi; önceki not korundu: " + ex.Message;
        }
    }

    private void Close()
    {
        if (FlushPending()) IsOpen = false;
    }

    private void Lock()
    {
        if (!FlushPending()) { Status = "Kaydedilmemiş değişiklik var; kasa kilitlenmedi."; return; }
        _debounce.Stop();
        _migrationPreview = null;
        MigrationConsent = false;
        IsOpen = false;
        _store?.Dispose();
        _store = null;
        _automaticBackupConsent.Reset();
        _draft = "";
        Changed(nameof(Draft));
        HasNote = false;
        Status = "Kasa kilitli. Parola belleğe kaydedilmez; eski düz metin notlar ayrı tutulur.";
    }

    public bool FlushPending() => !_dirty || Save();

    public void SecureCloseOnExit()
    {
        _debounce.Stop();
        _migrationPreview = null;
        MigrationConsent = false;
        _store?.Dispose();
        _store = null;
        _automaticBackupConsent.Reset();
        _draft = "";
        Changed(nameof(Draft));
        HasNote = false;
    }
}
