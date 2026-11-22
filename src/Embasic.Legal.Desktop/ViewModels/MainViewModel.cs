using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Embasic.Legal.Core;
using Embasic.Legal.Infrastructure;
using Microsoft.Win32;

namespace Embasic.Legal.Desktop.ViewModels;

public abstract class ObservableViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand(Action<object?> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute(parameter);
}

public sealed record NavigationItem(string Key, string Label);
public sealed record LawRow(string Number, string Title)
{
    public string DisplayName => Number + " — " + Title + "  [METİN DOĞRULANMADI]";
}
public sealed class DashboardViewModel { }
public sealed class CalculatorHubViewModel { }
public sealed record PlaceholderViewModel(string Title, string Message);

public sealed class LegislationViewModel : ObservableViewModel
{
    public IReadOnlyList<LawRow> Laws { get; } =
        [.. System.Linq.Enumerable.Select(LegislationCatalog.InitialDirectory,
            law => new LawRow(law.Number, law.Title))];

    private LawRow? _selectedLaw;
    public LawRow? SelectedLaw { get => _selectedLaw; set { _selectedLaw = value; Changed(); } }
    private string _receiptStatus =
        "Aday PDF alınmadı. Kaynak dizini resmî tam metin değildir; madde notları kapalıdır.";
    public string ReceiptStatus
    {
        get => _receiptStatus;
        private set { _receiptStatus = value; Changed(); }
    }

    public ICommand ImportPdfCommand { get; }
    public LegislationViewModel() =>
        ImportPdfCommand = new RelayCommand(_ => ImportCandidate());

    private void ImportCandidate()
    {
        if (SelectedLaw is null)
        {
            ReceiptStatus = "Önce bir mevzuat seçin (bu turda yalnız 5237 veya 5271).";
            return;
        }
        if (SelectedLaw.Number is not ("5237" or "5271"))
        {
            ReceiptStatus = "Diğer düzenlemelerin kaynak kabulü henüz açılmadı.";
            return;
        }
        var dialog = new OpenFileDialog
        {
            Title = "Resmî kaynak olduğu iddia edilen PDF adayını seç (yalnız yerel kanıt kaydı)",
            Filter = "PDF belgeleri (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var evidence = new LocalPdfEvidenceStore(LocalPdfEvidenceStore.DefaultDirectory);
            var result = evidence.Import(dialog.FileName, SelectedLaw.Number);
            var verifiedSnapshot = evidence.Recheck(result);
            ReceiptStatus = $"Aday dosya kaydı: {result.LawNumber} · {result.SizeBytes} bayt · SHA-256: " +
                result.Sha256 + "\nMakbuz geri okuma: " + verifiedSnapshot.Status +
                ". Yalnız BAYT BÜTÜNLÜĞÜ; resmî tamlık, madde–sayfa ve tarihsel yürürlük ONAYLANMADI.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                    or Microsoft.Data.Sqlite.SqliteException)
        {
            ReceiptStatus = "Kaynak adayı güvenle alınamadı: " + ex.Message;
        }
    }
}

public sealed class MainViewModel : ObservableViewModel
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
        new("home", "Ana Sayfa"),
        new("laws", "Mevzuat"),
        new("learning", "Öğretici Rehber · CMK 74+"),
        new("alternatives", "Alternatif Çözümler"),
        new("detention", "Tutuklama"),
        new("supervision", "Adli Kontrol"),
        new("search", "Arama ve El Koyma"),
        new("hub", "Adli Hesaplama Merkezi"),
        new("penalty", "  ├ Adli Ceza ve Para Cezası"),
        new("sentence", "      • Ceza oran hesaplama"),
        new("convert", "      • Hapis / para hesabı"),
        new("revalue", "      • YDO / para dönüşümü"),
        new("dates", "  ├ Belirli Tarih / Olay Tarihi"),
        new("shift", "  ├ Tarihe Gün / Ay / Yıl"),
        new("night", "  └ Gece Vakti"),
        new("notes", "Favoriler ve Notlar"),
        new("updates", "Güncellemeler"),
        new("settings", "Ayarlar ve Hakkında")
    ];
    private readonly NoteSandboxViewModel _notesSandbox = new();
    private readonly PenaltyMoneyViewModel _penalty = new();
    private readonly CmkLearningHubViewModel _learningHub = new();
    private readonly SystematicsPage _detention = new(ProceduralWorkflows.Detention, "Tutuklama");
    private readonly SystematicsPage _supervision = new(ProceduralWorkflows.JudicialControl, "Adli Kontrol");
    private readonly SystematicsPage _searchSeizure = new(ProceduralWorkflows.SearchSeizure, "Arama ve El Koyma");
    public bool HasPendingSystematicData =>
        _detention.HasReviewData || _supervision.HasReviewData || _searchSeizure.HasReviewData;
    public bool FlushNotesOnExit() => _notesSandbox.FlushPending();
    public void SecureCloseNotesOnExit() => _notesSandbox.SecureCloseOnExit();
    private object _currentPage = new DashboardViewModel();
    private string _currentTitle = "Ana Sayfa";
    public object CurrentPage { get => _currentPage; private set { _currentPage = value; Changed(); } }
    public string CurrentTitle { get => _currentTitle; private set { _currentTitle = value; Changed(); } }
    public ICommand NavigateCommand { get; }

    public MainViewModel() => NavigateCommand = new RelayCommand(value => Navigate(value as string));

    private PenaltyMoneyViewModel SetPenaltyTab(int tab)
    {
        _penalty.OpenTab(tab);
        return _penalty;
    }

    private void Navigate(string? key)
    {
        object page = key switch
        {
            "home" => new DashboardViewModel(),
            "hub" => new CalculatorHubViewModel(),
            "penalty" => _penalty,
            "sentence" => SetPenaltyTab(0),
            "convert" => SetPenaltyTab(1),
            "revalue" => SetPenaltyTab(2),
            "dates" => new DateRangeViewModel(),
            "shift" => new DateShiftViewModel(),
            "night" => new NightViewModel(),
            "laws" => new LegislationViewModel(),
            "learning" => _learningHub,
            "alternatives" => new PlaceholderViewModel("Alternatif Çözümler", "Kaynaklı hukuki karar destek modülü: sonraki turlarda. Üretim NO-GO."),
            "detention" => _detention,
            "supervision" => _supervision,
            "search" => _searchSeizure,
            "notes" => _notesSandbox,
            "updates" => new PlaceholderViewModel("Güncellemeler", "Resmî kaynak edinme ve sürüm kontrolü henüz etkin değil. Ağ erişimi otomatik başlatılmaz."),
            "settings" => new PlaceholderViewModel("Ayarlar ve Hakkında", "Embasic Legal 2.0 Alpha. WPF/.NET 10. Güvenilir hukuk içeriği denetimi bekleniyor."),
            _ => new DashboardViewModel()
        };
        CurrentPage = page;
        CurrentTitle = NavigationItems.FirstOrDefault(item => item.Key == key)?.Label.Trim().TrimStart('├', '└', ' ') ?? "Ana Sayfa";
    }
}

public static class DateInput
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    public static DateOnly ParseDate(string text)
    {
        if (!DateOnly.TryParseExact(text, "dd.MM.yyyy", Turkish, DateTimeStyles.None, out var value))
            throw new FormatException("Geçerli tarih biçimi gg.aa.yyyy olmalıdır.");
        return value;
    }
    public static int ParseAmount(string text)
    {
        if (!int.TryParse(text, NumberStyles.None, Turkish, out var n) || n < 0 || n > 9999)
            throw new FormatException("Gün/ay/yıl sayıları 0 ile 9999 arasında tam sayı olmalıdır.");
        return n;
    }
    public static string Format(DateOnly date) => date.ToString("dd.MM.yyyy", Turkish);
}

public sealed class DateShiftViewModel : ObservableViewModel
{
    public string Date { get; set; } = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    public string Years { get; set; } = "0";
    public string Months { get; set; } = "0";
    public string Days { get; set; } = "0";
    public bool Subtract { get; set; }
    private string _result = "Başlangıç tarihini ve süreyi giriniz.";
    public string Result { get => _result; private set { _result = value; Changed(); } }
    public ICommand CalculateCommand { get; }
    public ICommand CopyCommand { get; }

    public DateShiftViewModel()
    {
        CalculateCommand = new RelayCommand(_ => Calculate());
        CopyCommand = new RelayCommand(_ => Copy());
    }
    private void Copy()
    {
        try { System.Windows.Clipboard.SetText(Result); }
        catch (System.Runtime.InteropServices.ExternalException) { Result = "Panoya kopyalama başarısız."; }
    }
    private void Calculate()
    {
        try
        {
            var start = DateInput.ParseDate(Date);
            int direction = Subtract ? -1 : 1;
            var shifted = DateEngine.Shift(start,
                direction * DateInput.ParseAmount(Years),
                direction * DateInput.ParseAmount(Months),
                direction * DateInput.ParseAmount(Days));
            Result = $"{DateInput.Format(start)} → {DateInput.Format(shifted)}";
        }
        catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException)
        { Result = "Giriş hatası: " + e.Message; }
    }
}

public sealed class DateRangeViewModel : ObservableViewModel
{
    public string EventDate { get; set; } = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    public string ReferenceDate { get; set; } = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    public string Years { get; set; } = "0";
    public string Months { get; set; } = "0";
    public string Days { get; set; } = "0";
    public bool ApplyToReference { get; set; }
    public bool Subtract { get; set; }
    private string _result = "Olay ve belirli tarihleri giriniz.";
    public string Result { get => _result; private set { _result = value; Changed(); } }
    public ICommand CalculateCommand { get; }
    public ICommand CopyCommand { get; }
    public DateRangeViewModel()
    {
        CalculateCommand = new RelayCommand(_ => Calculate());
        CopyCommand = new RelayCommand(_ => Copy());
    }
    private void Copy()
    {
        try { System.Windows.Clipboard.SetText(Result); }
        catch (System.Runtime.InteropServices.ExternalException) { Result = "Panoya kopyalama başarısız."; }
    }

    private void Calculate()
    {
        try
        {
            DateOnly incident = DateInput.ParseDate(EventDate);
            DateOnly reference = DateInput.ParseDate(ReferenceDate);
            int sign = Subtract ? -1 : 1;
            DateOnly target = ApplyToReference ? reference : incident;
            DateOnly moved = DateEngine.Shift(target, sign * DateInput.ParseAmount(Years),
                sign * DateInput.ParseAmount(Months), sign * DateInput.ParseAmount(Days));
            if (ApplyToReference) reference = moved; else incident = moved;
            var span = DateEngine.Between(incident, reference);
            Result = $"Olay: {DateInput.Format(incident)} · Belirli tarih: {DateInput.Format(reference)}\n" +
                $"Fark: {span.Years} yıl {span.Months} ay {span.Days} gün · Toplam {span.TotalDays} gün. " +
                (span.Reversed ? "Olay tarihi belirli tarihten sonradır." : "Olay tarihi belirli tarihten önce veya eşittir.");
        }
        catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException)
        { Result = "Giriş hatası: " + e.Message; }
    }
}

public sealed class NightViewModel : ObservableViewModel
{
    public IReadOnlyList<string> Provinces { get; } =
        "Adana|Adıyaman|Afyonkarahisar|Ağrı|Aksaray|Amasya|Ankara|Antalya|Ardahan|Artvin|Aydın|Balıkesir|Bartın|Batman|Bayburt|Bilecik|Bingöl|Bitlis|Bolu|Burdur|Bursa|Çanakkale|Çankırı|Çorum|Denizli|Diyarbakır|Düzce|Edirne|Elazığ|Erzincan|Erzurum|Eskişehir|Gaziantep|Giresun|Gümüşhane|Hakkâri|Hatay|Iğdır|Isparta|İstanbul|İzmir|Kahramanmaraş|Karabük|Karaman|Kars|Kastamonu|Kayseri|Kırıkkale|Kırklareli|Kırşehir|Kilis|Kocaeli|Konya|Kütahya|Malatya|Manisa|Mardin|Mersin|Muğla|Muş|Nevşehir|Niğde|Ordu|Osmaniye|Rize|Sakarya|Samsun|Siirt|Sinop|Sivas|Şanlıurfa|Şırnak|Tekirdağ|Tokat|Trabzon|Tunceli|Uşak|Van|Yalova|Yozgat|Zonguldak".Split('|');
    public string Province { get; set; } = "Ankara";
    public string Date { get; set; } = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    public string EventTime { get; set; } = "";
    private string _result = "UYAP/Adalet Bakanlığı çizelgesinin kaynak baytları doğrulanmadı. Gece vakti hesaplanamaz.";
    public string Result { get => _result; private set { _result = value; Changed(); } }
    public ICommand CheckCommand { get; }
    public NightViewModel() => CheckCommand = new RelayCommand(_ =>
        Result = "Resmî çizelge verisi doğrulanamadı. Kaynak temin edilmeden güneş saatleri veya kesin gece vakti sonucu üretilemez.");
}
