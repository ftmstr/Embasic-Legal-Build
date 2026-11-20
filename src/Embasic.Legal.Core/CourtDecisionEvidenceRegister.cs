namespace Embasic.Legal.Core;

/// <summary>
/// A provenance register. This does not import or claim to certify official,
/// complete, enforceable court decisions. Published reproductions are not
/// court-certified originals; secondary descriptions are not decision texts.
/// </summary>
public enum CourtEvidenceLevel
{
    SecondarySummary,
    PublishedDecisionReproduction,
    OfficialCourtCertified
}

public sealed record CourtDecisionEvidence(
    string Key,
    string DecisionIdentity,
    string AffectedProvisions,
    string RecordedDisposition,
    string SourceUrl,
    string Publication,
    CourtEvidenceLevel Level,
    string OutstandingVerification)
{
    public bool OfficialDecisionOriginalVerified => false;
    public bool EntireDecisionAuthenticated => false;
    public bool EffectiveOnCaseDateVerified => false;
}

public sealed record CourtEvidenceAssessment(
    int RegisteredDecisions,
    int PublishedDecisionReproductions,
    int SecondaryOnlyDecisions,
    int UnverifiedOfficialOriginals,
    bool NormTimelineComplete,
    string Warning)
{
    public bool LegalEffectCertified => false;
    public bool CurrentConsolidatedRegulationVerified => false;
    public bool EnablesSearchOrSeizure => false;
}

public static class CourtDecisionEvidenceRegister
{
    // The official portal is a *search entry point*, NOT proof that a specific
    // decision's authenticated signed/original text has been retrieved.
    public static string OfficialSearchPortalUrl => "https://karararama.danistay.gov.tr/";
    public static string OfficialDocketPortalUrl => "https://dosyasorgulama.danistay.gov.tr/";
    public static string OfficialOriginalStatus =>
        "2007/948, 2012/1117 ve 2024/629 için tam, mahkeme kaynaklı kimliği teyit edilmiş karar aslı henüz edinilmedi. " +
        "Resmî arama portalının varlığı, aranan kararın indirildiğini veya yürürlük etkisinin doğrulandığını göstermez.";
    public static bool AllOfficialOriginalsRetrievedAndVerified => false;

    // Decision text (not official court-supplied PDF) reproduced in a professional publication.
    // Decision's dispositive part rejects cancellation requests concerning Art.8(c) and Art.27;
    // it is WRONG to label those provisions annulled by this decision.
    public static IReadOnlyList<CourtDecisionEvidence> Entries { get; } =
    [
        new("D2007",
            "Danıştay 10. Daire E.2005/6392 K.2007/948, 13.03.2007",
            "Yönetmelik m.8/1(a) belirli ibare, m.8/1(f) 'ilgilinin rızası' ibaresi ve m.30/1",
            "Belirtilen ibareler ve 30/1 iptal; m.8/1(c) ve m.27 iptal istemi reddi",
            "https://www.istanbulbarosu.org.tr/files/Yayinlar/Dergi/doc/ibd20075.pdf",
            "İstanbul Barosu Dergisi, 2007, Cilt 81, Sayı 5, basılı s.2417-2428",
            CourtEvidenceLevel.PublishedDecisionReproduction,
            "Danıştay kaynaklı imzalı/onaylı özgün karar sureti, PDF dosya tam hash'i ve sonraki norm sürümü yok"),
        new("D2012",
            "Danıştay İDDK E.2007/2257 K.2012/1117, 14.09.2012",
            "Yönetmelik m.8/1(a), m.8/1(f), m.30/1 iptal kararına karşı temyiz",
            "İptal kararının farklı gerekçeyle onandığı bildiriliyor",
            "https://istanbulbarosu.org.tr/haber/danistay-iddk-adli-arama-konusunda-adalet-bakanliginin-yonetmelik-duzenleme-yetkisi-bulunmamaktadir",
            "İstanbul Barosu haber özeti 12.07.2013 ve Bakanlık Yönetmelik PDF dipnotları",
            CourtEvidenceLevel.SecondarySummary,
            "İDDK kararının mahkemece yayımlanmış/kimlik doğrulamalı tam metni ve sonraki safahat elde edilmedi"),
        new("D2025",
            "Danıştay İDDK 23.01.2025, YD İtiraz No:2024/629",
            "Adlî ve Önleme Aramaları Yönetmeliği m.17/1",
            "Maddenin birinci fıkrasının yürütmesinin durdurulduğu ikincil metinde kayıtlı",
            "https://www.lexpera.com.tr/mevzuat/yonetmelikler/adli-ve-onleme-aramalari-yonetmeligi-1",
            "LE XPERA konsolide mevzuat dipnotu (birincil karar değil)",
            CourtEvidenceLevel.SecondarySummary,
            "Tam İDDK YD kararı, sonraki yargılama, varsa yeni düzenleme ve olay tarihli etki teyit edilmedi")
    ];

    public static CourtEvidenceAssessment Assess(DateOnly? incidentDate)
    {
        // No date and no manually supplied description can flip the legal-effect flag.
        int reproduced = Entries.Count(e => e.Level == CourtEvidenceLevel.PublishedDecisionReproduction);
        int secondary = Entries.Count(e => e.Level == CourtEvidenceLevel.SecondarySummary);
        return new(Entries.Count, reproduced, secondary, Entries.Count, false,
            incidentDate is null
                ? "Olay tarihi yok. Danıştay asıl karar tam metni ve mevzuat sürüm zinciri açık."
                : $"Olay tarihi {incidentDate:yyyy-MM-dd}; yalnız tarih girilmiş olması 2007 iptal, 2012 onama veya 2025 YD'nin somut dosya etkisini onaylamaz.");
    }
}

/// <summary>
/// Mechanical chronology with explicitly supplied document references.
/// The references are USER-ENTERED only, never verified legal documents.
/// </summary>
public sealed record SeizureDocumentInputs(
    DateTimeOffset? SeizedAt,
    string SeizureRecordReference,
    DateTimeOffset? SubmittedAt,
    string SubmissionRecordReference,
    DateTimeOffset? JudgeDecidedAt,
    string DecisionRecordReference);

public sealed record SeizureDocumentAudit(
    SeizureTimeComparison Clock,
    int MissingDocumentReferences,
    bool DocumentMetadataComplete,
    string Warning)
{
    public bool EvidenceAuthenticityVerified => false;
    public bool StatutoryComplianceVerified => false;
    // Even if every document reference is provided, a mechanical 24/48-hour
    // exceedance or missing timeline remains an explicit manual review flag.
    public bool MechanicalTimeAlert =>
        Clock.SubmittedWithin24h is MechanicalDeadlineResult.ExceedsReferenceWindow or
            MechanicalDeadlineResult.ChronologyConflict or MechanicalDeadlineResult.MissingEvidence ||
        Clock.JudgeDecisionWithin48h is MechanicalDeadlineResult.ExceedsReferenceWindow or
            MechanicalDeadlineResult.ChronologyConflict or MechanicalDeadlineResult.MissingEvidence;
}

public static class SeizureDocumentGate
{
    public static SeizureDocumentAudit Examine(SeizureDocumentInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var compare = SeizureClockAudit.Compare(inputs.SeizedAt,
            inputs.SubmittedAt, inputs.JudgeDecidedAt);
        int missing = 0;
        if (inputs.SeizedAt.HasValue && string.IsNullOrWhiteSpace(inputs.SeizureRecordReference)) missing++;
        if (inputs.SubmittedAt.HasValue && string.IsNullOrWhiteSpace(inputs.SubmissionRecordReference)) missing++;
        if (inputs.JudgeDecidedAt.HasValue && string.IsNullOrWhiteSpace(inputs.DecisionRecordReference)) missing++;
        bool complete = inputs.SeizedAt.HasValue && inputs.SubmittedAt.HasValue &&
                        inputs.JudgeDecidedAt.HasValue && missing == 0 &&
                        compare.SubmittedWithin24h != MechanicalDeadlineResult.ChronologyConflict &&
                        compare.JudgeDecisionWithin48h != MechanicalDeadlineResult.ChronologyConflict;
        bool timeAlert = compare.SubmittedWithin24h != MechanicalDeadlineResult.WithinReferenceWindow ||
                         compare.JudgeDecisionWithin48h != MechanicalDeadlineResult.WithinReferenceWindow;
        string warning = missing > 0
            ? $"{missing} belge için referans girilmedi. Zaman hesabı resmî evrakla doğrulanmış sayılamaz."
            : "Belge referansları yalnız kullanıcı beyanıdır; özgün evrak, imza ve kayıt saatleri doğrulanmadı.";
        if (timeAlert)
            warning += " DİKKAT: 24/48 saat mekanik aralığında eksik, kronoloji çelişkisi veya aşım riski; " +
                       "bu verilerle hukuki zaman uygunluğu kararı YASAK.";
        else
            warning += " Mekanik zaman aralığı içinde görünmek hukuki süreye uyulduğunun kanıtı değildir.";
        return new(compare, missing, complete, warning);
    }
}
