namespace Embasic.Legal.Core;

/// <summary>
/// A private, short-lived study prompt. This is a learning aid, not a statutory
/// assessment, evidence verification, medical opinion or legal authorization.
/// </summary>
public sealed record CmkReviewAgendaItem(
    string TopicKey,
    string Title,
    string ArticleLocator,
    int IncorrectFirstAnswers,
    int CorrectAfterFeedback,
    int RecallCardsMarkedReview,
    int UnansweredQuestions,
    int CheckedSteps,
    int TotalSteps)
{
    public string DisplayTitle => $"{Title} · {ArticleLocator}";
    public string ReviewReason =>
        $"İlk yanıtta yanlış: {IncorrectFirstAnswers} (sonradan doğru: {CorrectAfterFeedback}); " +
        $"tekrar işaretli kart: {RecallCardsMarkedReview}; " +
        $"henüz yanıtlanmamış soru: {UnansweredQuestions}; " +
        $"inceleme işareti: {CheckedSteps}/{TotalSteps}.";
    public string SourceStatus => "NOT_VERIFIED";
    public bool LegalComplianceCertified => false;
}

/// <summary>
/// Read-only projection of already existing in-memory quiz/card/checklist activity.
/// Unstarted lessons are NOT labeled as weak. No case content, disk, telemetry,
/// prediction of legal outcomes, score inflation or automatic source verification.
/// </summary>
public static class CmkReviewAgendaPlanner
{
    public static IReadOnlyList<CmkReviewAgendaItem> Build(
        IReadOnlyList<CmkLearningTopic> centralTopics,
        IReadOnlySet<string> answered,
        IReadOnlySet<string> firstCorrect,
        IReadOnlySet<string> recovered,
        Func<string, int, CmkRecallRating> rating,
        Func<string, int> checkedSteps)
    {
        ArgumentNullException.ThrowIfNull(centralTopics);
        ArgumentNullException.ThrowIfNull(answered);
        ArgumentNullException.ThrowIfNull(firstCorrect);
        ArgumentNullException.ThrowIfNull(recovered);
        ArgumentNullException.ThrowIfNull(rating);
        ArgumentNullException.ThrowIfNull(checkedSteps);
        var agenda = new List<CmkReviewAgendaItem>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var topic in centralTopics)
        {
            if (!keys.Add(topic.Key))
                throw new InvalidOperationException("Öğrenme merkezinde tekrar eden konu anahtarı var.");
            if (CmkCurriculum.AlternativeCategory.TopicKeys.Contains(topic.Key))
                throw new InvalidOperationException("Alternatif Çözümler merkezî tekrar gündemine karıştırılamaz.");

            var answeredIndices = Enumerable.Range(0, topic.Quiz.Count)
                .Where(i => answered.Contains($"{topic.Key}:{i}")).ToArray();
            int incorrect = answeredIndices.Count(i => !firstCorrect.Contains($"{topic.Key}:{i}"));
            int laterCorrect = answeredIndices.Count(i => recovered.Contains($"{topic.Key}:{i}")
                && !firstCorrect.Contains($"{topic.Key}:{i}"));
            int remaining = topic.Quiz.Count - answeredIndices.Length;
            var cards = CmkStudyCatalog.TryFind(topic.Key)?.Cards;
            int reviewCards = cards is null ? 0 :
                Enumerable.Range(0, cards.Count).Count(i => rating(topic.Key, i) == CmkRecallRating.Review);
            int checkedCount = checkedSteps(topic.Key);
            if (checkedCount < 0 || checkedCount > topic.Flow.Count)
                throw new InvalidOperationException("Kontrol listesi sayacı geçersiz.");

            // Never infer failure from an untouched topic. A self-assessed
            // Remembered card is NOT verified mastery and need not enter review.
            bool started = answeredIndices.Length > 0 || reviewCards > 0 || checkedCount > 0;
            bool unresolved = incorrect > 0 || reviewCards > 0 ||
                (started && remaining > 0) || (checkedCount > 0 && checkedCount < topic.Flow.Count);
            if (started && unresolved)
                agenda.Add(new(topic.Key, topic.Title, topic.ArticleLocator, incorrect,
                    laterCorrect, reviewCards, remaining, checkedCount, topic.Flow.Count));
        }
        return agenda
            .OrderByDescending(x => x.IncorrectFirstAnswers)
            .ThenByDescending(x => x.RecallCardsMarkedReview)
            .ThenByDescending(x => x.UnansweredQuestions)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToArray();
    }
}
