namespace Embasic.Legal.Core;

/// <summary>
/// Alert on a published Constitutional Court annulment affecting CMK 134.
/// The Official Gazette index verifies the existence/publication of the
/// decision, NOT that a complete official text, subsequent legislation or
/// historical consolidated norm has been certified for a particular case.
/// </summary>
public enum Cmk134RiskPeriod
{
    UnknownCaseDate,
    PrePublication,
    PublishedBeforeDeferredDate,
    OnOrAfterDeferredDate
}

public sealed record Cmk134ConstitutionalCourtReview(
    DateOnly? CaseDate,
    Cmk134RiskPeriod Period,
    string Warning,
    string GazetteIndexUrl,
    string DecisionReproductionUrl)
{
    public bool CompleteCourtDecisionVerified => false;
    public bool CurrentLegislationAfterJudgmentVerified => false;
    public bool OfficialCaseDateConsolidationVerified => false;
    public bool LegalStatusAutomaticallyCertified => false;
    public bool EnablesDigitalSearchAuthorization => false;
}

public static class Cmk134ConstitutionalCourtGate
{
    public static readonly DateOnly DecisionDate = new(2026, 2, 12);
    public static readonly DateOnly GazettePublicationDate = new(2026, 5, 25);
    public static readonly DateOnly ReportedDeferredEffectiveDate = new(2027, 2, 25);

    public const string GazetteIndexUrl = "https://www.resmigazete.gov.tr/25.05.2026";
    public const string DecisionReproductionUrl =
        "https://www.lexpera.com.tr/resmi-gazete/metin/anayasa-mahkemesinin-12-2-2026-tarihli-ve-e-2023-128-k-2026-36-sayili-karari-33264/70";

    public static Cmk134ConstitutionalCourtReview Assess(DateOnly? caseDate)
    {
        Cmk134RiskPeriod period;
        string advice;
        if (caseDate is null)
        {
            period = Cmk134RiskPeriod.UnknownCaseDate;
            advice = "Olay/işlem tarihi belirtilmedi: CMK 134 hakkında tarihsel norm incelemesi BAŞLATILAMADI.";
        }
        else if (caseDate.Value < GazettePublicationDate)
        {
            period = Cmk134RiskPeriod.PrePublication;
            advice = "İşlem tarihi AYM kararının Resmî Gazete yayımından önce. Geçmişteki CMK 134 norm sürümü ve işlem tarihindeki güvenceler ayrı incelenmeli.";
        }
        else if (caseDate.Value < ReportedDeferredEffectiveDate)
        {
            period = Cmk134RiskPeriod.PublishedBeforeDeferredDate;
            advice = "İşlem tarihi yayım sonrası, bildirilen dokuz aylık ileriye bırakılan yürürlükten önce. İptal kararı tek başına geçmiş işlemi otomatik geçersiz kılmaz. Olay tarihi ve devam eden mevzuat değişiklikleri doğrulanmalı.";
        }
        else
        {
            period = Cmk134RiskPeriod.OnOrAfterDeferredDate;
            advice = "İşlem tarihi 25.02.2027 ve sonrası: planlanan iptal yürürlüğü sınırına ulaşıldı. Sonraki kanun/değişiklik ve AYM kararının kesin kapsamı asıl resmî kaynaktan doğrulanmadan CMK 134'e göre işlem yetkisi VAR/YOK kararı VERİLEMEZ.";
        }

        return new(caseDate, period,
            "AYM 12.02.2026 E.2023/128 K.2026/36, RG 25.05.2026/33264: " +
            "CMK 134 dijital arama/kopyalama/elkoyma hükümlerine ilişkin iptal ve Resmî Gazete yayımından 9 ay sonra (25.02.2027) etkili olacağı bildirilen hüküm. " +
            advice + " Resmî Gazete KARAR LİSTESİ görüldü; tam kararın imzalı aslı, hüküm fıkralarının ayrıntılı kapsamı, takip eden düzenlemeler ve olay tarihi norm konsolidasyonu bu uygulamada TAMLIK PASS değil.",
            GazetteIndexUrl, DecisionReproductionUrl);
    }
}
