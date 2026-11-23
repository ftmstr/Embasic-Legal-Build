using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Embasic.Legal.Core;

namespace Embasic.Legal.Desktop.ViewModels;

public sealed record CmkLearningChoice(int Index, string Text)
{
    public string DisplayName => $"{Index + 1}. {Text}";
}

public sealed class CmkLearningChecklistRow : ObservableViewModel
{
    private bool _reviewed;
    public int Order { get; }
    public string Stage { get; }
    public string Prompt { get; }
    public bool Reviewed
    {
        get => _reviewed;
        set { if (_reviewed == value) return; _reviewed = value; Changed(); }
    }

    public CmkLearningChecklistRow(CmkLearningStep step, bool reviewed = false)
    {
        Order = step.Order;
        Stage = step.Stage;
        Prompt = step.ReviewPrompt;
        _reviewed = reviewed;
    }
}

/// <summary>
/// Fully offline, in-memory training only. A quiz score is NOT a finding that
/// legislation is current, that any document is authentic, or that a proposed
/// procedural measure is authorized. Never accepts personal/case narrative.
/// </summary>
public sealed class CmkLearningHubViewModel : ObservableViewModel
{
    private readonly HashSet<string> _answered = new(StringComparer.Ordinal);
    private readonly HashSet<string> _correct = new(StringComparer.Ordinal);
    private readonly HashSet<string> _recovered = new(StringComparer.Ordinal);
    private readonly CmkStudySession _studySession = new();
    private ObservableCollection<CmkRecallCardViewModel> _recallCards = [];
    private int _flowOrder = 1;
    // TUR025: checklist marks are owned by each topic for this RAM session.
    // Quiz scores are separate and cannot certify legal compliance.
    private readonly CmkLearningChecklistSession _checklistSession = new();
    private CmkLearningCategory _selectedCategory = CmkCurriculum.Categories[0];
    private CmkLearningTopic _selectedTopic = CmkCurriculum.All[0];
    private CmkLearningChoice? _selectedChoice;
    private int _questionIndex;
    private string _feedback = "Senaryoyu okuyup bir seçenek belirleyin. Puan yalnız öğrenme ilerlemesidir.";
    private ObservableCollection<CmkLearningChecklistRow> _checklist = [];

    private readonly IReadOnlyDictionary<string, SystematicsPage> _procedures;
    public bool IsAlternativeMode => SelectedCategory.Key == CmkCurriculum.AlternativeCategory.Key;
    public IReadOnlyList<CmkLearningCategory> Categories => IsAlternativeMode
        ? new[] { CmkCurriculum.AlternativeCategory } : CmkCurriculum.Categories;
    public SystematicsPage? Procedure => _procedures.TryGetValue(SelectedTopic.Key, out var page) ? page : null;
    public bool HasProcedure => Procedure is not null;
    // TUR032: must match the 0-based index of 'İşlem sistematiği' in App.xaml.
    // TUR030 added the 'Tekrar' tab; former index 6 now opens 'Kaynak'.
    public const int ProcedureTabIndex = 7;
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { if (_selectedTabIndex == value) return; _selectedTabIndex = value; Changed(); }
    }
    public void OpenCentre()
    {
        if (IsAlternativeMode) SelectedCategory = CmkCurriculum.Categories[0];
    }
    public CmkLearningCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (value is null || ReferenceEquals(_selectedCategory, value)) return;
            _selectedCategory = value;
            Changed();
            Changed(nameof(Topics));
            Changed(nameof(Categories));
            Changed(nameof(IsAlternativeMode));
            Changed(nameof(Title));
            Changed(nameof(CategorySummary));
            SelectedTopic = Topics[0];
        }
    }
    public IReadOnlyList<CmkLearningTopic> Topics => SelectedCategory.Topics;
    public string CategorySummary => IsAlternativeMode
        ? $"{Topics.Count} konu · {Topics.Sum(x => x.Quiz.Count)} eğitim sorusu · ayrı Alternatif Çözümler bölümü."
        : $"{Categories.Count} kategori · {CmkCurriculum.CentralTopics.Count} konu · " +
          $"{CmkCurriculum.CentralTopics.Sum(x => x.Quiz.Count)} eğitim sorusu. Seçili kategori: {Topics.Count} konu.";
    public CmkLearningLesson Lesson => CmkCurriculum.Explain(SelectedTopic.Key);
    public string RequestedDocumentUrl => CmkCurriculum.RequestedDocumentUrl;
    public string SourceRetrievalNote => CmkCurriculum.SourceRetrievalNote;
    public void OpenCategory(string key) => SelectedCategory = CmkCurriculum.FindCategory(key);
    public void OpenTopic(string topicKey)
    {
        // Old combined overview now opens the dedicated serial-procedure lesson.
        if (topicKey == "cmk250-253") topicKey = "cmk250";
        var topic = CmkLearningCatalog.Find(topicKey);
        SelectedCategory = CmkCurriculum.AllCategories.First(category => category.TopicKeys.Contains(topicKey));
        SelectedTopic = topic;
    }
    public CmkLearningTopic SelectedTopic
    {
        get => _selectedTopic;
        set
        {
            if (value is null || ReferenceEquals(_selectedTopic, value)) return;
            _selectedTopic = value;
            Changed();
            Changed(nameof(CurrentReference));
            Changed(nameof(Flow));
            Changed(nameof(Matrix));
            Changed(nameof(TopicStatus));
            Changed(nameof(Lesson));
            _flowOrder = 1;
            NotifyStudyChanged();
            RebuildRecallCards();
            Changed(nameof(Procedure));
            Changed(nameof(HasProcedure));
            SelectedTabIndex = 0;
            _questionIndex = 0;
            SelectedChoice = null;
            Feedback = "Yeni konu seçildi; önce şemayı ve tabloyu inceleyin.";
            RebuildChecklist();
            NotifyQuizChanged();
            NotifyReviewAgendaChanged();
        }
    }
    public CmkStudyProfile? Study => CmkStudyCatalog.TryFind(SelectedTopic.Key);
    public bool HasStudy => Study is not null;
    public bool NoStudy => !HasStudy;
    public IReadOnlyList<CmkLearningStep> Flow => Study?.Flow ?? SelectedTopic.Flow;
    public IReadOnlyList<CmkFlowChoice> FlowChoices => Flow.Select(step =>
        new CmkFlowChoice(step.Order, step.Stage, step.Order == _flowOrder)).ToArray();
    public CmkLearningStep ActiveFlow => Flow.First(step => step.Order == _flowOrder);
    public string FlowPosition => $"Adım {_flowOrder} / {Flow.Count}";
    public IReadOnlyList<CmkComparisonRow> Comparison => Study?.Comparison ?? [];
    public ObservableCollection<CmkRecallCardViewModel> RecallCards
    {
        get => _recallCards;
        private set { _recallCards = value; Changed(); Changed(nameof(RecallSummary)); }
    }
    public string RecallSummary => Study is null ? "" :
        $"Hatırladım: {Enumerable.Range(0, Study.Cards.Count).Count(i => _studySession.Rating(SelectedTopic.Key, i) == CmkRecallRating.Remembered)} / {Study.Cards.Count}; " +
        $"tekrar: {Enumerable.Range(0, Study.Cards.Count).Count(i => _studySession.Rating(SelectedTopic.Key, i) == CmkRecallRating.Review)}. " +
        "Bu kişisel özdeğerlendirmedir; öğrenme veya hukukî uygunluk belgesi değildir.";
    public IReadOnlyList<CmkBranchChoice> BranchChoices => Study is null ? [] :
        Study.BranchCase.Options.Select((option, i) => new CmkBranchChoice(i, option.Text,
            _studySession.Branch(SelectedTopic.Key) == i)).ToArray();
    public string BranchFeedback => Study is null ? "" : _studySession.Branch(SelectedTopic.Key) is int selected
        ? CmkStudyCatalog.AssessBranch(SelectedTopic.Key, selected).Feedback
        : "Önce olguyu okuyun; şemada bir yol seçip gerekçesini karşılaştırın.";
    private IReadOnlyList<CmkLearningQuizQuestion> CurrentQuestions => Study is null ? SelectedTopic.Quiz :
        Study.Quiz.Select(question => new CmkLearningQuizQuestion(question.Prompt,
            question.Options.Select(option => option.Text).ToArray(), question.CorrectIndex, "")).ToArray();
    public ICommand SelectFlowCommand { get; }
    public ICommand NextFlowCommand { get; }
    public ICommand BranchCommand { get; }
    private void NotifyStudyChanged()
    {
        foreach (var name in new[] { nameof(Study), nameof(HasStudy), nameof(NoStudy), nameof(FlowChoices),
            nameof(ActiveFlow), nameof(FlowPosition), nameof(Comparison), nameof(BranchChoices), nameof(BranchFeedback) }) Changed(name);
    }
    private void RebuildRecallCards()
    {
        RecallCards = new(Study?.Cards.Select((card, index) => new CmkRecallCardViewModel(card,
            SelectedTopic.Key, index, _studySession, () =>
            {
                Changed(nameof(RecallSummary));
                NotifyReviewAgendaChanged();
            })) ?? []);
    }
    private void SelectFlow(object? value)
    {
        if (value is not int order || !Flow.Any(step => step.Order == order)) return;
        _flowOrder = order; Changed(nameof(FlowChoices)); Changed(nameof(ActiveFlow)); Changed(nameof(FlowPosition));
    }
    private void SelectBranch(object? value)
    {
        if (Study is null || value is not int option) return;
        _studySession.SelectBranch(SelectedTopic.Key, option);
        Changed(nameof(BranchChoices)); Changed(nameof(BranchFeedback));
    }
    public IReadOnlyList<CmkLearningTableRow> Matrix => SelectedTopic.Matrix;
    public string CurrentReference => SelectedTopic.ArticleLocator;
    public string TopicStatus => SelectedTopic.SourceNotice;
    public string OfficialCmkUrl => CmkLearningCatalog.CurrentCmkUrl;
    public string OfficialTckUrl => CmkLearningCatalog.CurrentTckUrl;
    public string CanonicalSource => CmkLearningCatalog.CanonicalSource;
    public string Title => IsAlternativeMode ? "Alternatif Çözümler" : "Ceza Muhakemesi Hukuku";
    public string Warnings =>
        "EĞİTİM TASLAĞI / NOT_VERIFIED. Mevzuat.gov.tr güncel metni bu paketle eşleştirilmedi. " +
        "Madde başlıkları yol göstericidir, madde lafzı değildir. Süre, yetki, zorlayıcı tedbir " +
        "ve gerçek hukukî karar hesaplanmaz. Kişisel veya soruşturma verisi GİRMEYİN.";
    public ObservableCollection<CmkLearningChecklistRow> Checklist
    {
        get => _checklist;
        private set { _checklist = value; Changed(); Changed(nameof(ChecklistProgress)); }
    }
    public string ChecklistProgress => $"{Checklist.Count(x => x.Reviewed)} / {Checklist.Count} belge kontrolü " +
        "kullanıcı tarafından işaretlendi (asıl belge veya uygunluk doğrulanmadı).";
    public string QuestionTitle => $"Senaryo {_questionIndex + 1} / {CurrentQuestions.Count}";
    public string QuizPrompt => CurrentQuestions[_questionIndex].Prompt;
    public IReadOnlyList<CmkLearningChoice> Choices =>
        CurrentQuestions[_questionIndex].Choices
            .Select((text, index) => new CmkLearningChoice(index, text)).ToArray();
    public CmkLearningChoice? SelectedChoice
    {
        get => _selectedChoice;
        set { _selectedChoice = value; Changed(); }
    }
    public string Feedback
    {
        get => _feedback;
        private set { _feedback = value; Changed(); }
    }
    public string LearningProgress => $"{_answered.Count} / {CmkCurriculum.All.Sum(t => t.Quiz.Count)} senaryo yanıtlandı; " +
        $"İlk yanıtta doğru: {_correct.Count}; geri bildirimden sonra doğru: {_recovered.Count}. " +
        "Tekrarlar ilk yanıt puanını artırmaz. HUKUKÎ PASS / OTOMATİK KARAR YOK.";
    public string SessionLabel =>
        "Tamamen çevrimdışı ve RAM'de. Konu değiştirseniz de o konunun kontrol işaretleri " +
        "oturum sonuna veya Sıfırla'ya kadar korunur. Hukukî PASS yoktur.";

    // TUR030: topic-specific learning review list. It is recomputed from
    // in-memory activity, and never stores case data or claims legal mastery.
    public IReadOnlyList<CmkReviewAgendaItem> ReviewAgenda =>
        CmkReviewAgendaPlanner.Build(CmkCurriculum.CentralTopics, _answered, _correct,
            _recovered, _studySession.Rating, _checklistSession.ReviewedCount);
    public string ReviewAgendaSummary => ReviewAgenda.Count == 0
        ? "Henüz tekrar gerektirdiği işaretlenmiş bir konu yok. Çalışılmamış konular başarısız sayılmaz."
        : $"{ReviewAgenda.Count} konu tekrar listesinde. İlk yanlış yanıtlar ve 'Tekrar çalışacağım' " +
          "kartları önceliklidir. Bu liste başarı, kaynak veya hukukî uygunluk sertifikası değildir.";
    public ICommand OpenReviewTopicCommand { get; }
    private void NotifyReviewAgendaChanged()
    {
        Changed(nameof(ReviewAgenda));
        Changed(nameof(ReviewAgendaSummary));
    }
    private void OpenReviewTopic(object? value)
    {
        if (value is not string key || !CmkCurriculum.CentralTopics.Any(t => t.Key == key))
            return;
        // Preserve the common RAM session; route through the same curriculum
        // navigation as the sidebar, without creating duplicate lesson pages.
        OpenTopic(key);
        SelectedTabIndex = 0;
    }
    public ICommand AnswerCommand { get; }
    public ICommand NextQuestionCommand { get; }
    public ICommand ResetSessionCommand { get; }
    public ICommand CopyOfficialLinkCommand { get; }

    public CmkLearningHubViewModel(SystematicsPage detention, SystematicsPage supervision, SystematicsPage search)
    {
        _procedures = new Dictionary<string, SystematicsPage>(StringComparer.Ordinal)
        {
            ["cmk100-108"] = detention,
            ["cmk109-115"] = supervision,
            ["cmk116-122"] = search,
            ["cmk123-128"] = search,
            ["cmk128a"] = search,
            ["cmk129-133"] = search,
            ["cmk134"] = search
        };
        RebuildChecklist();
        RebuildRecallCards();
        SelectFlowCommand = new RelayCommand(SelectFlow);
        NextFlowCommand = new RelayCommand(_ => SelectFlow(_flowOrder % Flow.Count + 1));
        BranchCommand = new RelayCommand(SelectBranch);
        OpenReviewTopicCommand = new RelayCommand(OpenReviewTopic);
        AnswerCommand = new RelayCommand(_ => Answer());
        NextQuestionCommand = new RelayCommand(_ => Next());
        ResetSessionCommand = new RelayCommand(_ => Reset());
        CopyOfficialLinkCommand = new RelayCommand(_ => CopyOfficialLink());
    }
    private void RebuildChecklist()
    {
        // The old code created every checkbox as unchecked on topic changes,
        // silently discarding user learning progress while quiz score survived.
        // Rehydrate from independent, RAM-only per-topic marks; never disk.
        string topicKey = SelectedTopic.Key;
        var rows = new ObservableCollection<CmkLearningChecklistRow>(
            Flow.Select(step => new CmkLearningChecklistRow(
                step, _checklistSession.IsReviewed(topicKey, step.Order))));
        foreach (var row in rows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(CmkLearningChecklistRow.Reviewed)) return;
                _checklistSession.SetReviewed(topicKey, row.Order, row.Reviewed);
                if (SelectedTopic.Key == topicKey)
                {
                    Changed(nameof(ChecklistProgress));
                    NotifyReviewAgendaChanged();
                }
            };
        Checklist = rows;
    }
    private void NotifyQuizChanged()
    {
        Changed(nameof(QuestionTitle));
        Changed(nameof(QuizPrompt));
        Changed(nameof(Choices));
        Changed(nameof(LearningProgress));
    }
    private void Answer()
    {
        if (SelectedChoice is null)
        {
            Feedback = "Önce bir seçenek belirleyin. Bu işlem hukukî izin değerlendirmesi değildir.";
            return;
        }
        string key = $"{SelectedTopic.Key}:{_questionIndex}";
        bool firstAttempt = !_answered.Contains(key);
        if (!firstAttempt && Study is null)
        {
            Feedback = "Bu senaryo oturumda zaten yanıtlandı. Diğer senaryoya geçin veya oturumu sıfırlayın.";
            return;
        }
        var result = Study is null
            ? CmkLearningCatalog.Assess(SelectedTopic.Key, _questionIndex, SelectedChoice.Index)
            : CmkStudyCatalog.AssessQuiz(SelectedTopic.Key, _questionIndex, SelectedChoice.Index);
        _answered.Add(key);
        if (firstAttempt && result.Correct) _correct.Add(key);
        if (!firstAttempt && result.Correct && !_correct.Contains(key)) _recovered.Add(key);
        Feedback = result.Feedback + (!firstAttempt ? "\nBu bir tekrar denemesidir; ilk yanıt puanı değiştirilmedi." : "");
        Changed(nameof(LearningProgress));
        NotifyReviewAgendaChanged();
    }
    private void Next()
    {
        _questionIndex = (_questionIndex + 1) % CurrentQuestions.Count;
        SelectedChoice = null;
        Feedback = "Şema ve tabloya dönerek yeni senaryoyu cevaplayın. Kaynak NOT_VERIFIED.";
        NotifyQuizChanged();
    }
    private void Reset()
    {
        if (MessageBox.Show("Yalnız RAM'deki öğrenme puanı ve belge işaretleri silinsin mi?",
            "Embasic Legal — Eğitim Oturumu", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _answered.Clear();
        _correct.Clear();
        _recovered.Clear();
        _studySession.Reset();
        RebuildRecallCards();
        NotifyStudyChanged();
        // Reset every topic, not only the currently displayed checklist.
        _checklistSession.Reset();
        _questionIndex = 0;
        SelectedChoice = null;
        RebuildChecklist();
        Feedback = "Eğitim oturumu sıfırlandı. Mevzuat kaynağı hâlâ NOT_VERIFIED.";
        NotifyQuizChanged();
        NotifyReviewAgendaChanged();
    }
    private void CopyOfficialLink()
    {
        string url = SelectedTopic.Key == "tck75" ? OfficialTckUrl : RequestedDocumentUrl;
        try
        {
            Clipboard.SetText(url);
            Feedback = "Yalnız resmî mevzuat.gov.tr bağlantısı panoya kopyalandı. " +
                "Sayfadaki güncel metin ve olay tarihini ayrıca doğrulayın; uygulama otomatik erişmedi.";
        }
        catch (Exception)
        {
            Feedback = "Resmî kaynak bağlantısı panoya kopyalanamadı; URL ekran üzerinde görülebilir.";
        }
    }
}
