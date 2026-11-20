using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Embasic.Legal.Core;

namespace Embasic.Legal.Desktop.ViewModels;

/// <summary>Three legacy arithmetic tools, not a judicial decision engine.</summary>
public sealed class PenaltyMoneyViewModel : ObservableViewModel
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private int _selectedTab;
    private string _ratioResult = "Tam gün ve oran girin.";
    private string _fineResult = "Tam gün ve günlük tutar girin.";
    private string _revalueResult = "Yıllık YDO oranlarını manuel girin.";
    private string _historicRateNote = "Suç tarihi esaslı tarihsel günlük tutar bilgisi hesap sonrası gösterilir.";
    public int SelectedTab { get => _selectedTab; set { _selectedTab = value; Changed(); } }
    public string RatioDays { get; set; } = "180";
    public string Numerator { get; set; } = "1";
    public string Denominator { get; set; } = "6";
    public bool Reduction { get; set; } = true;
    public string RatioResult { get => _ratioResult; private set { _ratioResult = value; Changed(); } }
    public string FineDays { get; set; } = "90";
    public string DailyAmount { get; set; } = "100";
    public string AdditionalAmount { get; set; } = "0";
    public string CrimeDate { get; set; } = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    public string FineResult { get => _fineResult; private set { _fineResult = value; Changed(); } }
    public string HistoricRateNote { get => _historicRateNote; private set { _historicRateNote = value; Changed(); } }
    public string RevalueBase { get; set; } = "1000";
    public string RevalueRates { get; set; } = "";
    public string RateReference { get; set; } = "";
    public string OldLira { get; set; } = "1000000";
    public string RevalueResult { get => _revalueResult; private set { _revalueResult = value; Changed(); } }
    public ICommand CalculateRatioCommand { get; }
    public ICommand CalculateFineCommand { get; }
    public ICommand CalculateRevalueCommand { get; }
    public ICommand ConvertOldLiraCommand { get; }
    public ICommand CopyResultCommand { get; }

    public PenaltyMoneyViewModel()
    {
        CalculateRatioCommand = new RelayCommand(_ => CalculateRatio());
        CalculateFineCommand = new RelayCommand(_ => CalculateFine());
        CalculateRevalueCommand = new RelayCommand(_ => CalculateRevalue());
        ConvertOldLiraCommand = new RelayCommand(_ => ConvertOldLira());
        CopyResultCommand = new RelayCommand(_ => CopySelected());
    }
    public void OpenTab(int tab) => SelectedTab = tab;
    private static int PositiveInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, Turkish, out int n))
            throw new FormatException("Gün/pay/payda pozitif tam sayı olmalıdır.");
        return n;
    }
    private static decimal Amount(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new FormatException("Tutar boş bırakılamaz.");
        var style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        var culture = value.Contains(',') ? Turkish : CultureInfo.InvariantCulture;
        if (!decimal.TryParse(value, style, culture, out var n))
            throw new FormatException("Tutarı 1000 veya 100,50 biçiminde girin; binlik ayırıcı kullanmayın.");
        return n;
    }
    private void CalculateRatio()
    {
        try
        {
            var output = PenaltyMoneyArithmetic.ApplyFraction(
                PositiveInteger(RatioDays), PositiveInteger(Numerator),
                PositiveInteger(Denominator), Reduction);
            RatioResult = $"İlk süre: {output.OriginalDays} tam gün. {output.Method}. " +
                $"Matematiksel sonuç: {output.ResultDays} tam gün (fark {output.DifferenceDays:+#;-#;0} gün). " +
                "Yalnız tam gün girdisi işlenir; hapis süresindeki yıl ve ay otomatik günleştirilmez. " +
                "TCK 61/6, ceza alt/üst sınırları ve indirim sırası insan tarafından doğrulanmalıdır. " +
                "HUKUKİ CEZA BELİRLENMEDİ.";
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { RatioResult = "Oran hesabı reddedildi: " + ex.Message; }
    }
    private void CalculateFine()
    {
        try
        {
            var eventDate = DateInput.ParseDate(CrimeDate);
            var rate = PenaltyMoneyArithmetic.HistoricalRate(eventDate);
            var computed = PenaltyMoneyArithmetic.MultiplyDays(
                PositiveInteger(FineDays), Amount(DailyAmount), Amount(AdditionalAmount));
            FineResult = $"{computed.Days} tam gün × " +
                $"{computed.DailyAmount.ToString("N2", Turkish)} TL = {computed.Multiplication.ToString("N2", Turkish)} TL. " +
                $"Tam TL bileşeni (TCK 61/6 aritmetik): {computed.WholeLiraComponent.ToString("N2", Turkish)} TL. " +
                $"Başka tutarla yalnız matematiksel toplam: {computed.IllustrativeTotal.ToString("N2", Turkish)} TL. " +
                computed.Warning;
            HistoricRateNote = "Suç tarihi: " + eventDate.ToString("dd.MM.yyyy", Turkish) +
                ". " + rate.SourceAndLimitation +
                (rate.HistoricalMinimum is null ? "" :
                    $" Tarihsel bilgi aralığı: {rate.HistoricalMinimum}–{rate.HistoricalMaximum} TL/gün.") +
                " Mahkeme takdiri, özel suç hükmü, lehe kanun, dönüşüm koşulları ve sonraki değişiklikler otomatik onaylanmadı.";
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { FineResult = "Adlî para hesabı reddedildi: " + ex.Message; }
    }
    private void CalculateRevalue()
    {
        try
        {
            var original = Amount(RevalueBase);
            var factors = PenaltyMoneyArithmetic.ParseManualRates(RevalueRates);
            var sum = PenaltyMoneyArithmetic.CompoundRevaluation(original, factors);
            RevalueResult = $"Başlangıç {original.ToString("N2", Turkish)} TL. " +
                $"Girdiğiniz {factors.Count} dönemlik YDO'nun ardışık aritmetiği: " +
                $"{sum.ToString("N2", Turkish)} TL (ham sonuç {sum.ToString(CultureInfo.InvariantCulture)}). " +
                (string.IsNullOrWhiteSpace(RateReference)
                    ? "Oranın resmî kaynak referansı boş: KAYNAK DOĞRULANMADI. "
                    : $"Kullanıcının yazdığı kaynak (bağımsız doğrulanmadı): {RateReference}. ") +
                "YDO, TCK 52 günlük tutarına veya adlî para cezasına otomatik uygulanamaz. " +
                "Kanuni yuvarlama, sınırlama, geçiş, istisna veya uygulanabilirlik kararı verilmedi.";
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { RevalueResult = "YDO matematiği reddedildi: " + ex.Message; }
    }
    private void ConvertOldLira()
    {
        try
        {
            decimal old = Amount(OldLira);
            var lira = PenaltyMoneyArithmetic.OldLiraToLira(old);
            RevalueResult = $"{old.ToString("N0", Turkish)} eski TL = " +
                $"{lira.ToString("N2", Turkish)} TL (1 Ocak 2005 parasal birim değişimi, 1.000.000:1). " +
                "Bu enflasyon, bugünkü alım gücü veya dava tarihindeki hukuki tutar değildir.";
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { RevalueResult = "Para birimi dönüşümü reddedildi: " + ex.Message; }
    }
    private void CopySelected()
    {
        var text = SelectedTab switch { 0 => RatioResult, 1 => FineResult + "\n" + HistoricRateNote,
            _ => RevalueResult };
        if (MessageBox.Show("Yalnız matematiksel örnek panoya kopyalanacak. Gerçek dosya veya kişisel veri girmeyin. Devam?",
            "Embasic Legal — Pano Uyarısı", MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Clipboard.SetText(text); }
        catch (ExternalException) { RevalueResult = "Pano erişimi başarısız."; }
    }
}
