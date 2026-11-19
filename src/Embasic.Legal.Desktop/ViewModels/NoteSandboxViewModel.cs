using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Embasic.Legal.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Embasic.Legal.Desktop.ViewModels;

/// <summary>
/// Interactive WPF bubble/sticker for a synthetic article only.
/// A real article is not selectable until an independently verified corpus and source-version contract exist.
/// </summary>
public sealed class NoteSandboxViewModel : ObservableViewModel
{
    private readonly ArticleNoteKey _key =
        new("SANDBOX", "SYNTHETIC-ARTICLE", "", "TUR003-EXPLICIT-DEMO");
    private readonly DispatcherTimer _debounce;
    private ArticleNoteStore? _store;
    private bool _dirty;
    private bool _hasNote;
    private bool _isOpen;
    private bool _expanded;
    private string _draft = "";
    private string _status = "Sentetik örnek hazır; gerçek soruşturma veya kişisel veri girmeyin.";

    public NoteSandboxViewModel()
    {
        ToggleCommand = new RelayCommand(_ => Toggle());
        ExpandCommand = new RelayCommand(_ => Expand());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete());
        CloseCommand = new RelayCommand(_ => Close());
        _debounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(900)
        };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Save(); };
        try
        {
            _store = new ArticleNoteStore(ArticleNoteStore.DefaultDatabasePath);
            var saved = _store.Get(_key);
            if (saved is not null)
            {
                _draft = saved.Text;
                _hasNote = true;
                _status = "Önceden kaydedilmiş sentetik not geri yüklendi.";
            }
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            _status = "SQLite not alanı açılamadı; kayıt güvenli biçimde durduruldu: " + ex.Message;
            _store?.Dispose();
            _store = null;
        }
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
                Status = "Değişiklik bekliyor; otomatik kayıt 0,9 saniye sonra denenecek.";
                _debounce.Start();
            }
            else Status = "Boş not kaydedilmez; kaldırmak için Sil düğmesini kullanın.";
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

    private void Toggle()
    {
        if (IsOpen) Close();
        else if (_store is null) Status = "SQLite kullanılamıyor; boş düzenleyici açılmadı.";
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
            Status = "Boş not kaydedilmez. Önceki kayıt silinmedi.";
            return false;
        }
        if (_store is null)
        {
            Status = "Veritabanı yok. Taslak korunuyor, kaydedildi denilemez.";
            return false;
        }

        try
        {
            _store.Save(_key, Draft);
            _dirty = false;
            HasNote = true;
            try
            {
                _store.BackupTo(ArticleNoteStore.DefaultDatabasePath + ".backup.sqlite3");
                Status = "Not kalıcı kaydedildi ve bütünlük kontrollü yedeği oluşturuldu.";
            }
            catch (Exception ex) when (ex is IOException or SqliteException or UnauthorizedAccessException)
            {
                Status = "Not kaydedildi, ancak yedek alınamadı: " + ex.Message;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or SqliteException or UnauthorizedAccessException)
        {
            Status = "Not kaydedilemedi. Taslak hâlâ açık: " + ex.Message;
            return false;
        }
    }

    private void Delete()
    {
        if (_store is null) { Status = "Silme yapılamaz: SQLite erişilemiyor."; return; }
        if (MessageBox.Show("Sentetik not silinsin mi? Önce mevcut kayıt yedeklenecek.",
                "Embasic Legal — Deneme Notu", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes) return;
        try
        {
            _debounce.Stop();
            // Preserve last committed note before destructive operation.
            _store.BackupTo(ArticleNoteStore.DefaultDatabasePath + ".backup.sqlite3");
            _store.Delete(_key);
            _draft = "";
            Changed(nameof(Draft));
            _dirty = false;
            HasNote = false;
            Status = "Deneme notu silindi; önceki not yedekte tutuldu.";
        }
        catch (Exception ex) when (ex is IOException or SqliteException or UnauthorizedAccessException)
        {
            Status = "Silme tamamlanmadı; kayıt korundu: " + ex.Message;
        }
    }

    private void Close()
    {
        if (FlushPending()) IsOpen = false;
    }

    public bool FlushPending() => !_dirty || Save();
}
