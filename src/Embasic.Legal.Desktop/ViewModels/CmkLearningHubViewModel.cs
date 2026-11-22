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

    public CmkLearningChecklistRow(CmkLearningStep step)
    {
        Order = step.Order;
        Stage = step.Stage;
        Prompt = step.ReviewPrompt;
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
    private CmkLearningTopic _selectedTopic = CmkLearningCatalog.All[0];
    private CmkLearningChoice? _selectedChoice;
    private int _questionIndex;
    private string _feedback = "Senaryoyu okuyup bir seçenek belirleyin. Puan yalnız öğrenme ilerlemesidir.";
    private ObservableCollection<CmkLearningChecklistRow> _checklist = [];

    public IReadOnlyList<CmkLearningTopic> Topics => CmkLearningCatalog.All;
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
    public string Title => "Koruma Tedbirleri ve Adlî İşlemler — Öğrenme Merkezi";
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
    public string LearningProgress => $"{_answered.Count} / {Topics.Sum(t => t.Quiz.Count)} senaryo yanıtlandı; " +
        $"{_correct.Count} öğrenme yanıtı doğru. HUKUKÎ PASS / OTOMATİK KARAR YOK.";
    public string SessionLabel => "Tamamen çevrimdışı ve RAM'de; kişisel dosya kaydı oluşturulmaz.";
    public ICommand AnswerCommand { get; }
    public ICommand NextQuestionCommand { get; }
    public ICommand ResetSessionCommand { get; }
    public ICommand CopyOfficialLinkCommand { get; }

    public CmkLearningHubViewModel()
    {
        RebuildChecklist();
        AnswerCommand = new RelayCommand(_ => Answer());
        NextQuestionCommand = new RelayCommand(_ => Next());
        ResetSessionCommand = new RelayCommand(_ => Reset());
        CopyOfficialLinkCommand = new RelayCommand(_ => CopyOfficialLink());
    }
    private void RebuildChecklist()
    {
        var rows = new ObservableCollection<CmkLearningChecklistRow>(
            SelectedTopic.Flow.Select(s => new CmkLearningChecklistRow(s)));
        foreach (var row in rows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CmkLearningChecklistRow.Reviewed))
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
        _questionIndex = 0;
        SelectedChoice = null;
        RebuildChecklist();
        Feedback = "Eğitim oturumu sıfırlandı. Mevzuat kaynağı hâlâ NOT_VERIFIED.";
        NotifyQuizChanged();
    }
    private void CopyOfficialLink()
    {
        string url = SelectedTopic.Key == "tck75" ? OfficialTckUrl : OfficialCmkUrl;
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
