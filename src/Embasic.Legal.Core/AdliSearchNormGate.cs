using System.Globalization;

namespace Embasic.Legal.Core;

/// <summary>
/// Structured risk ledger, NOT an official current-law or court-decision certification.
/// Sources describe possible risks; they cannot make official legal-effect flags true.
/// </summary>
public sealed record NormDispute(
    string Id,
    string Title,
    string AffectedCheck,
    string Warning,
    string Reference,
    string DiscoveryUrl,
    string EvidenceGrade)
{
    public bool CertifiedPrimaryDecision => false;
    public bool EffectiveOnCaseDateProved => false;
}

public sealed record NormRiskReview(
    DateOnly? CaseDate,
    bool CaseDateKnown,
    int UnresolvedRisks,
    string Summary)
{
    public bool CurrentOfficialTextVerified => false;
    public bool HistoricalApplicabilityVerified => false;
    public bool AuthorizesLegalCompliancePass => false;
}

public static class AdliSearchNormGate
{
    public static IReadOnlyList<NormDispute> Risks { get; } =
    [
        new("R08A", "Yönetmelik 8/1(a) iptal ibaresi", "S09",
            "Konut, işyeri, yerleşim yeri, eklenti ve araçta yakalama amacıyla kararsız aramayı genişleten tarihsel ibare iptal dipnotuna konu. Olay tarihi ve hüküm kapsamı bağımsız incelenmeli.",
            "Danıştay 10. D., 13.03.2007 E.2005/6392 K.2007/948; İDDK 14.09.2012 E.2007/2257 K.2012/1117",
            "https://cigm.adalet.gov.tr/Resimler/SayfaDokuman/1222021100307Adli%20ve%20%C3%96nleme%20Aramalar%C4%B1%20Y%C3%B6netmeli%C4%9Fi.pdf",
            "Bakanlık PDF dipnotu; asıl Danıştay kararının tam metni/sürümü doğrulanmadı"),
        new("R08F", "Yönetmelik 8/1(f) rıza ibaresinin iptali", "S09",
            "Yalnız ilgilinin rızası ibaresi iptal dipnotunda. Rıza tek başına her adlî aramaya kanuni yetki oluşturmuş sayılmamalı.",
            "Danıştay 10. D., 13.03.2007 E.2005/6392 K.2007/948; İDDK 14.09.2012 E.2007/2257 K.2012/1117",
            "https://cigm.adalet.gov.tr/Resimler/SayfaDokuman/1222021100307Adli%20ve%20%C3%96nleme%20Aramalar%C4%B1%20Y%C3%B6netmeli%C4%9Fi.pdf",
            "Bakanlık PDF dipnotu; asıl Danıştay kararı olay tarihine göre ayrıca incelenmeli"),
        new("R30", "Yönetmelik 30/1 iptal hükmü", "S14",
            "Aramanın icrasına ilişkin genel bölümde yer alan 30/1 fıkrası iptal dipnotunda. Adlî bölüm 5-17 ile ortak icra hükümleri 27-32 ayrı tasnif edilmeli.",
            "Danıştay 10. D., 13.03.2007 E.2005/6392 K.2007/948; İDDK 14.09.2012 E.2007/2257 K.2012/1117",
            "https://cigm.adalet.gov.tr/Resimler/SayfaDokuman/1222021100307Adli%20ve%20%C3%96nleme%20Aramalar%C4%B1%20Y%C3%B6netmeli%C4%9Fi.pdf",
            "Bakanlık PDF dipnotu; asıl karar ve sonraki işlemler incelenmedi"),
        new("R17", "Yönetmelik 17/1 yürütmeyi durdurma kaydı", "S24",
            "23.01.2025 tarihli İDDK YD İtiraz No 2024/629 kaydı ikinci kaynakta yer alıyor. CMK 134 ile kanun-yönetmelik norm hiyerarşisi ve kararın güncel safahatı bağımsız teyit gerektirir.",
            "Danıştay İDDK, 23.01.2025, YD İtiraz No: 2024/629 (ikincil konsolide metindeki kayıt)",
            "https://www.lexpera.com.tr/mevzuat/yonetmelikler/adli-ve-onleme-aramalari-yonetmeligi-1",
            "İkincil konsolide kaynak; birincil mahkeme karar dosyası edinilmedi"),
        new("RDATE", "Olay tarihi / norm sürümü / geçiş etkisi", "S27",
            "Yönetmeliğin 2005 orijinal ve 2016 değişiklikleri; tarihsel düzenleme, iptal, YD, CMK ve 7571 değişiklikleri ayrıştırılmalı. Olay tarihi verilse bile kendiliğinden uygunluk kanıtı olmaz.",
            "CMK m.116-138; Adlî ve Önleme Aramaları Yönetmeliği m.5-17 ve 27-32; olay tarihli RG ve Danıştay kararı",
            "https://mevzuat.adalet.gov.tr/mevzuat/102946",
            "UYAP sayfa referansı; olay tarihli tam sürüm, karar safahatı ve PDF tamlık manifesti yok")
    ];

    public static NormRiskReview Assess(DateOnly? eventDate)
    {
        // A date by itself never establishes which disputed text was legally applicable.
        var message = eventDate is null
            ? "Olay tarihi belirtilmedi. Yönetmelik iptal/YD, CMK değişiklik ve tarihsel norm sürümü incelemesi GEREKLİ."
            : $"Olay tarihi: {eventDate:yyyy-MM-dd}. Bu tarih yalnız kaydedildi; birincil Danıştay kararları, değişikliklerin yürürlüğü ve güncel norm tamlığı doğrulanmadı.";
        return new(eventDate, eventDate.HasValue, Risks.Count, message);
    }
}

public enum MechanicalDeadlineResult
{
    MissingEvidence,
    ChronologyConflict,
    WithinReferenceWindow,
    ExceedsReferenceWindow
}

/// <summary>Elapsed instant checks for CMK 127/3's 24h and 48h reference windows.
/// Not an interpretation of a case's effective version, exclusions or time proof.</summary>
public sealed record SeizureTimeComparison(
    MechanicalDeadlineResult SubmittedWithin24h,
    MechanicalDeadlineResult JudgeDecisionWithin48h,
    string Summary)
{
    public bool LegalDeadlineCertified => false;
    public bool OfficialDocumentTimestampsVerified => false;
    public bool AutomaticSeizureReleaseDecision => false;
}

public static class SeizureClockAudit
{
    public static SeizureTimeComparison Compare(
        DateTimeOffset? seizedAt, DateTimeOffset? submittedAt, DateTimeOffset? judgeDecidedAt)
    {
        if (seizedAt is null)
            return new(MechanicalDeadlineResult.MissingEvidence,
                MechanicalDeadlineResult.MissingEvidence, "Elkoyma anına ilişkin belgeli tarih-saat eksik.");
        if ((submittedAt.HasValue && submittedAt.Value < seizedAt.Value) ||
            (judgeDecidedAt.HasValue && judgeDecidedAt.Value < seizedAt.Value) ||
            (submittedAt.HasValue && judgeDecidedAt.HasValue &&
                judgeDecidedAt.Value < submittedAt.Value))
            return new(MechanicalDeadlineResult.ChronologyConflict,
                MechanicalDeadlineResult.ChronologyConflict, "Zaman sıralaması çelişkili; dosyadaki orijinal belgeler karşılaştırılmalı.");

        var a = submittedAt.HasValue
            ? (submittedAt.Value - seizedAt.Value > TimeSpan.FromHours(24)
                ? MechanicalDeadlineResult.ExceedsReferenceWindow
                : MechanicalDeadlineResult.WithinReferenceWindow)
            : MechanicalDeadlineResult.MissingEvidence;
        var b = judgeDecidedAt.HasValue
            ? (judgeDecidedAt.Value - seizedAt.Value > TimeSpan.FromHours(48)
                ? MechanicalDeadlineResult.ExceedsReferenceWindow
                : MechanicalDeadlineResult.WithinReferenceWindow)
            : MechanicalDeadlineResult.MissingEvidence;

        return new(a, b, $"CMK 127/3 için yalnız aritmetik karşılaştırma: hâkime sunum/24 saat = {a}; " +
            $"hâkim kararının elkoymadan itibaren 48 saati = {b}. " +
            "Karar, istisna, tebligat, dosya ve olay tarihindeki norm hukuki olarak ayrıca incelenmeli; otomatik uygunluk veya kalkma kararı yok.");
    }

    public static bool TryParseLocalWithOffset(string text, out DateTimeOffset timestamp) =>
        DateTimeOffset.TryParseExact(text,
            ["yyyy-MM-ddTHH:mmzzz", "yyyy-MM-ddTHH:mm:sszzz"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp);
}
