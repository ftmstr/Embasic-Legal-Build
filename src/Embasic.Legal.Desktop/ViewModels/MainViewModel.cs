using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Embasic.Legal.Core;

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
public sealed class DashboardViewModel;
public sealed class CalculatorHubViewModel;
public sealed record PlaceholderViewModel(string Title, string Message);

public sealed class LegislationViewModel
{
    public IReadOnlyList<LawRow> Laws { get; } =
        [.. System.Linq.Enumerable.Select(LegislationCatalog.InitialDirectory,
            law => new LawRow(law.Number, law.Title))];
}

public sealed class MainViewModel : ObservableViewModel
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
        new("home", "Ana Sayfa"),
        new("laws", "Mevzuat"),
        new("alternatives", "Alternatif Çözümler"),
        new("detention", "Tutuklama"),
        new("supervision", "Adli Kontrol"),
        new("search", "Arama ve El Koyma"),
        new("hub", "Adli Hesaplama Merkezi"),
        new("dates", "  ├ Belirli Tarih / Olay Tarihi"),
        new("shift", "  ├ Tarihe Gün / Ay / Yıl"),
        new("night", "  └ Gece Vakti"),
        new("notes", "Favoriler ve Notlar"),
        new("updates", "Güncellemeler"),
        new("settings", "Ayarlar ve Hakkında")
    ];
    private object _currentPage = new DashboardViewModel();
    private string _currentTitle = "Ana Sayfa";
    public object CurrentPage { get => _currentPage; private set { _currentPage = value; Changed(); } }
    public string CurrentTitle { get => _currentTitle; private set { _currentTitle = value; Changed(); } }
    public ICommand NavigateCommand { get; }

    public MainViewModel() => NavigateCommand = new RelayCommand(value => Navigate(value as string));

    private void Navigate(string? key)
    {
        (CurrentTitle, CurrentPage) = key switch
        {
            "home" => ("Ana Sayfa", new DashboardViewModel()),
            "hub" => ("Adli Hesaplama Merkezi", new CalculatorHubViewModel()),
            "dates" => ("Belirli Tarih / Olay Tarihi", new DateRangeViewModel()),
            "shift" => ("Tarihe Gün / Ay / Yıl", new DateShiftViewModel()),
            "night" => ("Gece Vakti", new NightViewModel()),
            "laws" => ("Mevzuat", new LegislationViewModel()),
            "alternatives" => ("Alternatif Çözümler", new PlaceholderViewModel("Alternatif Çözümler", "Kaynaklı hukuki karar destek modülü: sonraki turlarda. Üretim NO-GO.")),
            "detention" => ("Tutuklama", new PlaceholderViewModel("Tutuklama", "Doğrulanmış CMK içeriği gelene kadar otomatik karar üretilmez.")),
            "supervision" => ("Adli Kontrol", new PlaceholderViewModel("Adli Kontrol", "Doğrulanmış mevzuat ve insan incelemesi gereklidir.")),
            "search" => ("Arama ve El Koyma", new PlaceholderViewModel("Arama ve El Koyma", "Adli ve önleme araması ayrı ele alınacaktır. Modül henüz etkin değil.")),
            "notes" => ("Favoriler ve Notlar", new PlaceholderViewModel("Favoriler ve Notlar", "Madde notu baloncuğu ve sticker altyapısı TUR 001 kapsamı dışında; planlandı.")),
            "updates" => ("Güncellemeler", new PlaceholderViewModel("Güncellemeler", "Resmî kaynak edinme ve sürüm kontrolü henüz etkin değil. Ağ erişimi otomatik başlatılmaz.")),
            "settings" => ("Ayarlar ve Hakkında", new PlaceholderViewModel("Ayarlar ve Hakkında", "Embasic Legal 2.0 Alpha. WPF/.NET 10. Güvenilir hukuk içeriği denetimi bekleniyor.")),
            _ => ("Ana Sayfa", new DashboardViewModel())
        };
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

    public DateShiftViewModel() => CalculateCommand = new RelayCommand(_ => Calculate());
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
    public DateRangeViewModel() => CalculateCommand = new RelayCommand(_ => Calculate());

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
