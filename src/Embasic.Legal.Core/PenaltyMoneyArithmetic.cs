using System.Globalization;

namespace Embasic.Legal.Core;

/// <summary>Mathematical aid. Never authorizes imprisonment, conversion or a legal sentence.</summary>
public sealed record PenaltyArithmeticResult(
    int OriginalDays, int ResultDays, int DifferenceDays, string Method)
{
    public bool JudicialSentenceValidated => false;
    public bool StatutoryMinimumMaximumVerified => false;
    public bool FavorableLawVerified => false;
}

public sealed record FineArithmeticResult(
    int Days, decimal DailyAmount, decimal AdditionalAmount,
    decimal Multiplication, decimal WholeLiraComponent, decimal IllustrativeTotal,
    string Warning)
{
    public bool JudicialConversionAllowed => false;
    public bool SentencingRangeVerified => false;
    public bool EnforcementAuthorized => false;
}

public sealed record HistoricalDailyRateInfo(
    DateOnly CrimeDate, decimal? HistoricalMinimum, decimal? HistoricalMaximum,
    string SourceAndLimitation)
{
    public bool FavorableLawReviewed => false;
    public bool SpecialStatuteReviewed => false;
    public bool DateSpecificApplicationCertified => false;
}

public static class PenaltyMoneyArithmetic
{
    public static PenaltyArithmeticResult ApplyFraction(
        int wholeDays, int numerator, int denominator, bool reduce)
    {
        if (wholeDays is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(wholeDays), "Tam gün 1–1.000.000 arasında olmalıdır.");
        if (denominator is < 1 or > 1000 || numerator is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(numerator), "Pay ve payda 1–1000 arasında olmalıdır.");
        if (reduce && numerator > denominator)
            throw new ArgumentException("İndirim oranı %100'ü aşamaz.");
        // Truncate the final day count, not the intermediate reduction itself.
        long product = (long)wholeDays * (reduce
            ? denominator - numerator : denominator + numerator);
        int result = checked((int)(product / denominator));
        if (result > 1_000_000) throw new ArgumentOutOfRangeException(nameof(wholeDays),
            "Sonuç güvenli hesap aralığını aşıyor.");
        return new(wholeDays, result, result - wholeDays,
            reduce ? $"Oransal indirim {numerator}/{denominator}" :
                     $"Oransal artırım {numerator}/{denominator}");
    }

    public static FineArithmeticResult MultiplyDays(
        int days, decimal amountPerDay, decimal additionalPenalty = 0)
    {
        if (days is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(days));
        if (amountPerDay <= 0 || amountPerDay > 1_000_000m)
            throw new ArgumentOutOfRangeException(nameof(amountPerDay));
        if (additionalPenalty < 0 || additionalPenalty > 1_000_000_000m)
            throw new ArgumentOutOfRangeException(nameof(additionalPenalty));
        decimal product = checked(days * amountPerDay);
        // TCK 61/6: a whole-TL result is shown as an illustration; the
        // separate additional amount's source/aggregation is NOT certified.
        decimal wholeLira = decimal.Truncate(product);
        return new(days, amountPerDay, additionalPenalty, product,
            wholeLira, checked(wholeLira + additionalPenalty),
            "Yalnız aritmetik. TCK 50/52 koşulları, 5–730 genel gün sınırının özel hüküm istisnaları, " +
            "TCK 61/6 artakalan ve suç tarihindeki lehe kanun insan incelemesi gerektirir.");
    }

    public static HistoricalDailyRateInfo HistoricalRate(DateOnly crimeDate)
    {
        if (crimeDate < new DateOnly(2005, 6, 1))
            return new(crimeDate, null, null,
                "5237 sayılı TCK'nın başlangıç döneminden önce otomatik günlük tutar önerisi yok.");
        if (crimeDate < new DateOnly(2024, 6, 1))
            return new(crimeDate, 20, 100,
                "Tarihsel TCK 52/2 metninde 20–100 TL. 7499 m.9 değişikliğinin yürürlüğü 01.06.2024; " +
                "TCK 7 lehe kanun ve dosyadaki özel norm doğrulanmadan uygulanabilir tutar belirlenmez.");
        return new(crimeDate, 100, 500,
            "7499 sayılı Kanun m.9, m.40(1)(a), RG 12.03.2024/32487: 01.06.2024 sonrası metinde " +
            "TCK 52/2 için 100–500 TL. Olay tarihli lehe kanun ve sonraki değişiklikler ayrıca incelenmelidir.");
    }

    public static decimal CompoundRevaluation(decimal original, IReadOnlyList<decimal> rates)
    {
        if (original < 0 || original > 1_000_000_000_000m)
            throw new ArgumentOutOfRangeException(nameof(original));
        if (rates is null || rates.Count is < 1 or > 25)
            throw new ArgumentException("1–25 açıkça girilmiş YDO oranı gereklidir.");
        decimal value = original;
        foreach (var rate in rates)
        {
            if (rate is < 0 or > 1000)
                throw new ArgumentOutOfRangeException(nameof(rates),
                    "Her YDO 0 ile 1000 yüzde arasında olmalıdır.");
            value = checked(value * (1 + rate / 100m));
        }
        return value;
    }

    public static IReadOnlyList<decimal> ParseManualRates(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Resmî kaynaktan doğrulanmış oranları manuel girin.");
        // A semicolon separates years. A comma can be a Turkish decimal separator.
        string[] parts = text.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length > 25 || parts.Any(string.IsNullOrWhiteSpace))
            throw new FormatException("Oranları noktalı virgülle ayırın: 43,93; 25,49.");
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var result = new List<decimal>(parts.Length);
        foreach (var part in parts)
        {
            var culture = part.Contains(',') ? tr : CultureInfo.InvariantCulture;
            if (!decimal.TryParse(part, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    culture, out var rate))
                throw new FormatException("YDO sayı biçimi geçersiz: " + part);
            if (rate < 0 || rate > 1000) throw new FormatException("YDO oranı 0–1000 aralığında olmalı.");
            result.Add(rate);
        }
        return result;
    }

    public static decimal OldLiraToLira(decimal oldLira)
    {
        if (oldLira < 0 || oldLira > 1_000_000_000_000_000m)
            throw new ArgumentOutOfRangeException(nameof(oldLira));
        return oldLira / 1_000_000m;
    }
}
