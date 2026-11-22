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
            Changed(nameof(Procedure));
            Changed(nameof(HasProcedure));
            SelectedTabIndex = 0;
            _questionIndex = 0;
            SelectedChoice = null;
            Feedback = "Yeni konu seçildi; önce şemayı ve tabloyu inceleyin.";
            RebuildChecklist();
            NotifyQuizChanged();
        }
    }
    public IReadOnlyList<CmkLearningStep> Flow => SelectedTopic.Flow;
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
    public string QuestionTitle => $"Senaryo {_questionIndex + 1} / {SelectedTopic.Quiz.Count}";
    public string QuizPrompt => SelectedTopic.Quiz[_questionIndex].Prompt;
    public IReadOnlyList<CmkLearningChoice> Choices =>
        SelectedTopic.Quiz[_questionIndex].Choices
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
        $"{_correct.Count} öğrenme yanıtı doğru. HUKUKÎ PASS / OTOMATİK KARAR YOK.";
    public string SessionLabel =>
        "Tamamen çevrimdışı ve RAM'de. Konu değiştirseniz de o konunun kontrol işaretleri " +
        "oturum sonuna veya Sıfırla'ya kadar korunur. Hukukî PASS yoktur.";
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
            SelectedTopic.Flow.Select(step => new CmkLearningChecklistRow(
                step, _checklistSession.IsReviewed(topicKey, step.Order))));
        foreach (var row in rows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(CmkLearningChecklistRow.Reviewed)) return;
                _checklistSession.SetReviewed(topicKey, row.Order, row.Reviewed);
                if (SelectedTopic.Key == topicKey)
                    Changed(nameof(ChecklistProgress));
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
        if (_answered.Contains(key))
        {
            Feedback = "Bu senaryo oturumda zaten yanıtlandı. Diğer senaryoya geçin veya oturumu sıfırlayın.";
            return;
        }
        var result = CmkLearningCatalog.Assess(
            SelectedTopic.Key, _questionIndex, SelectedChoice.Index);
        _answered.Add(key);
        if (result.Correct) _correct.Add(key);
        Feedback = result.Feedback;
        Changed(nameof(LearningProgress));
    }
    private void Next()
    {
        _questionIndex = (_questionIndex + 1) % SelectedTopic.Quiz.Count;
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
        // Reset every topic, not only the currently displayed checklist.
        _checklistSession.Reset();
        _questionIndex = 0;
        SelectedChoice = null;
        RebuildChecklist();
        Feedback = "Eğitim oturumu sıfırlandı. Mevzuat kaynağı hâlâ NOT_VERIFIED.";
        NotifyQuizChanged();
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
