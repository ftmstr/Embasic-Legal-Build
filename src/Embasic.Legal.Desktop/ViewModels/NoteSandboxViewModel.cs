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
        PreviewLegacyCommand = new RelayCommand(_ => PreviewLegacy());
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
    public ICommand PreviewLegacyCommand { get; }
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
            EncryptedArticleNotes.RestoreFrom(dialog.FileName,
                EncryptedArticleNotes.DefaultDatabasePath, pass);
            Status = "Geri yükleme bütünlük ve parola doğrulamasından geçti. Açmak için parolayı tekrar girin.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or SqliteException or CryptographicException or ArgumentException or FormatException)
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
            try
            {
                _store.BackupTo(EncryptedArticleNotes.DefaultDatabasePath + ".backup.sqlite3");
                Status = "Not şifreli kaydedildi ve bütünlük kontrollü yedeklendi.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
            {
                Status = "Şifreli not kaydedildi; ancak yedek başarısız: " + ex.Message;
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
        try
        {
            _debounce.Stop();
            _store.BackupTo(EncryptedArticleNotes.DefaultDatabasePath + ".backup.sqlite3");
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
        _draft = "";
        Changed(nameof(Draft));
        HasNote = false;
    }
}
