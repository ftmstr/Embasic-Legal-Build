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
    private readonly ArithmeticDraftGuard _ratioGuard = new();
    private readonly ArithmeticDraftGuard _fineGuard = new();
    private readonly ArithmeticDraftGuard _revalueGuard = new();

    private string _ratioDays = "180", _numerator = "1", _denominator = "6";
    private bool _reduction = true;
    private string _fineDays = "90", _dailyAmount = "100", _additionalAmount = "0";
    private string _crimeDate = DateInput.Format(DateOnly.FromDateTime(DateTime.Today));
    private string _revalueBase = "1000", _revalueRates = "", _rateReference = "";
    private string _oldLira = "1000000";

    // All mutable inputs invalidate the previously computed evidence.
    // This prevents a copied output from silently referring to earlier input.
    private bool Update(ref string storage, string next, Action invalidate,
        [System.Runtime.CompilerServices.CallerMemberName] string? property = null)
    {
        if (storage == next) return false;
        storage = next ?? "";
        Changed(property);
        invalidate();
        return true;
    }
    private void InvalidateRatio()
    {
        _ratioGuard.InputChanged();
        RatioResult = "Girdi değişti. Önceki ceza oran sonucu GEÇERSİZ. Yeniden hesaplayın.";
    }
    private void InvalidateFine()
    {
        _fineGuard.InputChanged();
        FineResult = "Girdi değişti. Önceki adlî para sonucu GEÇERSİZ. Yeniden hesaplayın.";
        HistoricRateNote = "Suç tarihi / günlük tutar değişti. Önceki tarihsel oran bilgisi GEÇERSİZ.";
    }
    private void InvalidateRevalue()
    {
        _revalueGuard.InputChanged();
        RevalueResult = "Girdi veya kaynak beyanı değişti. Önceki YDO / para dönüşümü GEÇERSİZ.";
    }
    public string RatioDays { get => _ratioDays; set => Update(ref _ratioDays, value, InvalidateRatio); }
    public string Numerator { get => _numerator; set => Update(ref _numerator, value, InvalidateRatio); }
    public string Denominator { get => _denominator; set => Update(ref _denominator, value, InvalidateRatio); }
    public bool Reduction
    {
        get => _reduction;
        set { if (_reduction == value) return; _reduction = value; Changed(); InvalidateRatio(); }
    }
    public string RatioResult { get => _ratioResult; private set { _ratioResult = value; Changed(); } }

    public string FineDays { get => _fineDays; set => Update(ref _fineDays, value, InvalidateFine); }
    public string DailyAmount { get => _dailyAmount; set => Update(ref _dailyAmount, value, InvalidateFine); }
    public string AdditionalAmount { get => _additionalAmount; set => Update(ref _additionalAmount, value, InvalidateFine); }
    public string CrimeDate { get => _crimeDate; set => Update(ref _crimeDate, value, InvalidateFine); }
    public string FineResult { get => _fineResult; private set { _fineResult = value; Changed(); } }
    public string HistoricRateNote { get => _historicRateNote; private set { _historicRateNote = value; Changed(); } }

    public string RevalueBase { get => _revalueBase; set => Update(ref _revalueBase, value, InvalidateRevalue); }
    public string RevalueRates { get => _revalueRates; set => Update(ref _revalueRates, value, InvalidateRevalue); }
    public string RateReference { get => _rateReference; set => Update(ref _rateReference, value, InvalidateRevalue); }
    public string OldLira { get => _oldLira; set => Update(ref _oldLira, value, InvalidateRevalue); }
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
        _ratioGuard.InputChanged();
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
            _ratioGuard.CalculationSucceeded();
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException or FormatException)
        { RatioResult = "Oran hesabı reddedildi: " + ex.Message; }
    }
    private void CalculateFine()
    {
        _fineGuard.InputChanged();
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
            _fineGuard.CalculationSucceeded();
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException or FormatException)
        { FineResult = "Adlî para hesabı reddedildi: " + ex.Message; }
    }
    private void CalculateRevalue()
    {
        _revalueGuard.InputChanged();
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
            _revalueGuard.CalculationSucceeded();
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException or FormatException)
        { RevalueResult = "YDO matematiği reddedildi: " + ex.Message; }
    }
    private void ConvertOldLira()
    {
        _revalueGuard.InputChanged();
        try
        {
            decimal old = Amount(OldLira);
            var lira = PenaltyMoneyArithmetic.OldLiraToLira(old);
            RevalueResult = $"{old.ToString("N0", Turkish)} eski TL = " +
                $"{lira.ToString("N2", Turkish)} TL (1 Ocak 2005 parasal birim değişimi, 1.000.000:1). " +
                "Bu enflasyon, bugünkü alım gücü veya dava tarihindeki hukuki tutar değildir.";
            _revalueGuard.CalculationSucceeded();
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException or FormatException)
        { RevalueResult = "Para birimi dönüşümü reddedildi: " + ex.Message; }
    }
    private void CopySelected()
    {
        var eligible = SelectedTab switch
        {
            0 => _ratioGuard.HasCurrentResult,
            1 => _fineGuard.HasCurrentResult,
            2 => _revalueGuard.HasCurrentResult,
            _ => false
        };
        if (!eligible)
        {
            MessageBox.Show("Bu sekmede geçerli, son girdiye ait hesap sonucu yok. Önce yeniden hesaplayın.",
                "Embasic Legal — Eski Sonuç Engellendi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var text = SelectedTab switch { 0 => RatioResult, 1 => FineResult + "\n" + HistoricRateNote,
            _ => RevalueResult };
        if (MessageBox.Show("Yalnız matematiksel örnek panoya kopyalanacak. Gerçek dosya veya kişisel veri girmeyin. Devam?",
            "Embasic Legal — Pano Uyarısı", MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Clipboard.SetText(text); }
        catch (ExternalException) { RevalueResult = "Pano erişimi başarısız."; }
    }
}
