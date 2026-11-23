namespace Embasic.Legal.Core;

public sealed record CmkComparisonRow(string Criterion, string Left, string Right, string Reason);
public sealed record CmkRecallCard(string Question, string Answer, string ArticleLocator);
public sealed record CmkStudyOption(string Text, string Explanation);
public sealed record CmkStudyQuestion(string Prompt, IReadOnlyList<CmkStudyOption> Options, int CorrectIndex);
public sealed record CmkStudyProfile(string TopicKey, string LeftLabel, string RightLabel,
    IReadOnlyList<string> Objectives, string ArticleReadingGuide,
    IReadOnlyList<CmkComparisonRow> Comparison, IReadOnlyList<CmkRecallCard> Cards,
    IReadOnlyList<CmkLearningStep> Flow, CmkStudyQuestion BranchCase,
    IReadOnlyList<CmkStudyQuestion> Quiz)
{
    public string ComparisonTitle => LeftLabel + " ↔ " + RightLabel;
    public string SourceStatus => "NOT_VERIFIED";
}

/// <summary>Original topic-specific conceptual exercises, not a verified statutory consolidation.</summary>
public static partial class CmkStudyCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, CmkStudyProfile>> Index = new(() =>
        BuildProfiles().ToDictionary(p => p.TopicKey, StringComparer.Ordinal));
    public static IReadOnlyList<CmkStudyProfile> All => Array.AsReadOnly(Index.Value.Values.ToArray());
    public static CmkStudyProfile? TryFind(string key) => Index.Value.TryGetValue(key, out var p) ? p : null;
    public static CmkStudyProfile Find(string key) => TryFind(key)
        ?? throw new InvalidOperationException("Bu konuda etkin öğrenme profili yok.");

    private static CmkStudyProfile Create(string key, string left, string right,
        CmkComparisonRow[] rows, string scenario, string correct, string wrong1, string wrong2)
    {
        var topic = CmkLearningCatalog.Find(key);
        if (rows.Length != 3) throw new InvalidOperationException("Üç karşılaştırma ekseni gerekir.");
        int rotate = Array.IndexOf(CmkCurriculum.CentralTopics.Select(t => t.Key).ToArray(), key) % 3;
        CmkStudyQuestion MakeQuestion(string prompt, string answer, string nearMiss, string overgeneralization, int position)
        {
            var choices = new CmkStudyOption[3];
            choices[position] = new(answer, "Bu seçim ayrımı korur. " + rows[0].Reason + " " + rows[1].Reason);
            choices[(position + 1) % 3] = new(nearMiss,
                "Bu çıkarımda ayrım kayboluyor. " + left + ": " + rows[0].Left + " " + right + ": " + rows[0].Right + " " + rows[0].Reason);
            choices[(position + 2) % 3] = new(overgeneralization,
                "Bu genelleme iki ayrı incelemeyi tek sonuca indirger. " + rows[1].Reason + " " + rows[2].Reason);
            return new(prompt, Array.AsReadOnly(choices), position);
        }
        var branch = MakeQuestion(scenario, correct, wrong1, wrong2, rotate);
        var transfer = TransferCase(key);
        var assessment = MakeQuestion(transfer.Prompt, transfer.Correct, transfer.NearMiss,
            transfer.Generalization, (rotate + 2) % 3);
        var contrast = MakeQuestion("Kavramları kendiniz eşleştirin: hangi açıklama doğru ilişkiyi kurar?",
            left + ": " + rows[0].Left + " / " + right + ": " + rows[0].Right,
            left + ": " + rows[0].Right + " / " + right + ": " + rows[0].Left,
            left + " ile " + right + " aynı kavramdır; " + rows[0].Criterion.ToLowerInvariant() + " yönünden ayrı inceleme gerekmez.",
            (rotate + 1) % 3);
        string[] objectives = [left + " ile " + right + " ayrımını kendi cümlelerinizle açıklamak.",
            rows[1].Criterion + " ve " + rows[2].Criterion.ToLowerInvariant() + " bakımından farkı uygulamak.",
            "Senaryodaki çıkarımı gerekçelendirmek ve yanlış seçeneğin hatasını göstermek."];
        var cards = rows.Select(row => new CmkRecallCard(
            left + " ile " + right + " arasında «" + row.Criterion + "» bakımından fark nedir?",
            left + ": " + row.Left + "\n" + right + ": " + row.Right + "\nNeden önemli? " + row.Reason,
            topic.ArticleLocator)).ToArray();
        string guide = topic.ArticleLocator + " okuma rehberi: «" + rows[0].Criterion + "», «" +
            rows[1].Criterion + "» ve «" + rows[2].Criterion + "» sorularına ayrı cevap arayın. " +
            "Bu referans bir madde lafzı veya güncel yürürlük doğrulaması değildir; metin mevzuat.gov.tr üzerinden ayrıca okunmalıdır.";
        var flow = topic.Flow;
        // Earlier practice modules had generic four-stage headings. Give these
        // a topic-specific conceptual sequence without changing procedure gates.
        if (CmkLearningCatalog.All.Any(t => t.Key == key))
        {
            string[] stages = ["1 · " + rows[0].Criterion, "2 · " + rows[1].Criterion,
                "3 · " + rows[2].Criterion, "4 · Senaryoda ayrımı uygula"];
            flow = Array.AsReadOnly(Enumerable.Range(0, 4).Select(i => new CmkLearningStep(i + 1, stages[i],
                i < 3 ? left + ": " + rows[i].Left + " " + right + ": " + rows[i].Right + " " + rows[i].Reason
                    : scenario + "\nÖnce kendi cevabınızı kurun; sonra aşağıdaki dallardan birini seçin.",
                topic.ArticleLocator + " — okuma referansı; güncel metin doğrulanmadı",
                "İNSAN KONTROLÜ — eğitim açıklaması, somut işlem izni değil")).ToArray());
        }
        return new(key, left, right, Array.AsReadOnly(objectives), guide, Array.AsReadOnly(rows),
            Array.AsReadOnly(cards), flow, branch, Array.AsReadOnly(new[] { assessment, contrast }));
    }

    private static CmkLearningResult Assess(CmkStudyQuestion question, int option)
    {
        if (option < 0 || option >= question.Options.Count) throw new ArgumentOutOfRangeException(nameof(option));
        bool correct = option == question.CorrectIndex;
        return new(correct, (correct ? "EĞİTİM SEÇİMİ DOĞRU. " : "BU ÇIKARIMI YENİDEN İNCELEYİN. ") +
            question.Options[option].Explanation + "\nGerekçeli yaklaşım: " + question.Options[question.CorrectIndex].Text +
            "\nGüncel resmî metin NOT_VERIFIED; hukukî karar veya yetki onayı değildir.", false, "NOT_VERIFIED");
    }
    public static CmkLearningResult AssessBranch(string key, int option) => Assess(Find(key).BranchCase, option);
    public static CmkLearningResult AssessQuiz(string key, int questionIndex, int option)
    {
        var profile = Find(key);
        if (questionIndex < 0 || questionIndex >= profile.Quiz.Count) throw new ArgumentOutOfRangeException(nameof(questionIndex));
        return Assess(profile.Quiz[questionIndex], option);
    }
}
