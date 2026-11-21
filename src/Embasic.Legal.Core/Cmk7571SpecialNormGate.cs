namespace Embasic.Legal.Core;

/// <summary>
/// 7571 m.22 / m.26 is a separate legal-source review boundary; it does not
/// infer measure authorization, past applicability or transitional outcome.
/// Only source-independent date/identity triage is performed.
/// </summary>
public static class Cmk7571SpecialNormGate
{
    public static readonly DateOnly PublicationAndGeneralEffectiveDate = new(2025, 12, 25);
    private static readonly HashSet<string> Listed128AOffenses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "142/2-E", "158/1-F", "158/1-L", "245"
        };

    public sealed record Cmk128AReview(
        string SpecialNorm, bool OffenseInEnumeratedList,
        string MeasureDatePosition, bool IncidentPredatesMeasureLaw,
        bool RequiresSpecialDocumentReview)
    {
        public bool AutomaticSeizureOrSuspensionAuthorization => false;
        public bool OfficialCaseSpecificCorpusVerified => false;
        public bool GenericCmk127DeadlineCanCertifyThisMeasure => false;
    }

    /// <param name="incidentDate">Historical alleged incident date; never a proxy for measure date.</param>
    /// <param name="measureDate">Date of account suspension/seizure procedural act.</param>
    /// <param name="tckArticleVariant">Exact normalized identity, e.g. 158/1-F; no broad 158 shortcut.</param>
    public static Cmk128AReview ReviewCmk128A(
        DateOnly? incidentDate, DateOnly? measureDate, string? tckArticleVariant)
    {
        string code = tckArticleVariant?.Trim().ToUpperInvariant() ?? "";
        bool listed = Listed128AOffenses.Contains(code);
        string position = measureDate is null ? "MEASURE_DATE_MISSING" :
            measureDate.Value < PublicationAndGeneralEffectiveDate ?
                "BEFORE_2025_12_25" : "ON_OR_AFTER_2025_12_25";
        return new Cmk128AReview("7571-m22-CMK128A", listed, position,
            incidentDate.HasValue &&
            incidentDate.Value < PublicationAndGeneralEffectiveDate &&
            measureDate.HasValue &&
            measureDate.Value >= PublicationAndGeneralEffectiveDate,
            true);
    }

    public sealed record Temporary8Review(
        string SpecialNorm, string SettlementCutoffPosition,
        bool CasePendingAtEffectiveDateVerified,
        bool? HistoricalTransitionCandidate)
    {
        public bool AutomaticTransitionOrMediationOutcome => false;
        public bool OfficialCaseSpecificCorpusVerified => false;
    }

    /// <summary>
    /// A calendar date ON publication day cannot prove "itibarıyla uzlaşmanın
    /// sağlanmış olduğu" without original event/timing evidence. Even when
    /// candidate=true the law's applicability is not certified.
    /// </summary>
    public static Temporary8Review ReviewCmkTemporary8(
        DateOnly? settlementDate, bool? investigationOrTrialPendingAtEffectiveDate)
    {
        string position = settlementDate is null ? "SETTLEMENT_DATE_MISSING" :
            settlementDate.Value < PublicationAndGeneralEffectiveDate ?
                "BEFORE_2025_12_25" :
            settlementDate.Value == PublicationAndGeneralEffectiveDate ?
                "ON_EFFECTIVE_DATE_REQUIRES_TIME_PROOF" : "AFTER_2025_12_25";
        bool pending = investigationOrTrialPendingAtEffectiveDate == true;
        bool? candidate = !investigationOrTrialPendingAtEffectiveDate.HasValue ||
            settlementDate is null ||
            settlementDate.Value == PublicationAndGeneralEffectiveDate
                ? null
                : investigationOrTrialPendingAtEffectiveDate == false ||
                  settlementDate.Value > PublicationAndGeneralEffectiveDate
                    ? false
                    : pending && settlementDate.Value < PublicationAndGeneralEffectiveDate;
        return new Temporary8Review("7571-m26-CMK-Gecici8", position,
            pending, candidate);
    }
}
