namespace Embasic.Legal.Core;

/// <summary>
/// Explicit human declaration of whether the seizure was initially ordered by
/// a judge. The selection is never treated as an authenticated court order.
/// </summary>
public enum SeizureAuthorizationBasis
{
    NotEstablished = 0,
    PriorJudicialOrderDeclared = 1,
    WithoutPriorJudicialOrderDeclared = 2
}

public enum SeizureClockApplicability
{
    RequiresBasisEvidence = 0,
    PriorOrderReportedNo127ThirdParagraphCheck = 1,
    WarrantlessReferenceReviewOnly = 2
}

public sealed record SeizureAuthorizationReview(
    SeizureAuthorizationBasis DeclaredBasis,
    SeizureClockApplicability Applicability,
    SeizureDocumentAudit? TimeAudit,
    string Warning)
{
    public bool BasisDocumentAuthenticityVerified => false;
    public bool NormVersionVerified => false;
    public bool CourtDecisionCertified => false;
    public bool LegalConsequenceApproved => false;
    public bool HasMechanicalTimeComparison => TimeAudit is not null;
}

public static class SeizureAuthorizationGate
{
    public static SeizureAuthorizationReview Assess(
        SeizureAuthorizationBasis basis,
        SeizureDocumentInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return basis switch
        {
            SeizureAuthorizationBasis.NotEstablished =>
                new(basis, SeizureClockApplicability.RequiresBasisEvidence, null,
                    "Elkoymanın önceden hâkim kararına dayanıp dayanmadığı kanıtla ayrılmadı. " +
                    "CMK 127/3 24/48 saat hesabı başlatılamaz. Ön karar, yazılı emir ve işlem evrakı kontrol edilmelidir."),
            SeizureAuthorizationBasis.PriorJudicialOrderDeclared =>
                new(basis, SeizureClockApplicability.PriorOrderReportedNo127ThirdParagraphCheck, null,
                    "KULLANICI BEYANI: Önceden hâkim kararı olduğu işaretlendi. " +
                    "CMK 127/3'ün kararsız elkoyma 24/48 saat aralığı otomatik uygulanmaz. " +
                    "Kararın işlemden önce varlığı, kapsamı, uygulanabilirliği ve gerçekliği doğrulanmadı."),
            SeizureAuthorizationBasis.WithoutPriorJudicialOrderDeclared =>
                new(basis, SeizureClockApplicability.WarrantlessReferenceReviewOnly,
                    SeizureDocumentGate.Examine(inputs),
                    "KULLANICI BEYANI: Hâkim kararı olmadan elkoyma işaretlendi. " +
                    "CMK 127/3 yalnız mekanik 24/48 saat referans karşılaştırmasına açıldı. " +
                    "Emrin yetkisi, olay tarihi, resmî evrak, bildirimin zamanında yapılması " +
                    "ve hakimin kararının açıklanma zamanı ayrı kanıtlanmalı."),
            _ => new(SeizureAuthorizationBasis.NotEstablished,
                SeizureClockApplicability.RequiresBasisEvidence, null,
                "Bilinmeyen elkoyma dayanağı kodu reddedildi. Saat hesabı yapılmaz.")
        };
    }
}
