using System.Globalization;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Embasic.Legal.Core;

namespace Embasic.Legal.Desktop.ViewModels;

public sealed record StatusOption(ProceduralEvidenceState Code, string DisplayName);

public sealed class SystematicRow : ObservableViewModel
{
    private readonly Action _update;
    private ProceduralEvidenceState _state;
    private string _evrak = "";
    public ProceduralCheck Check { get; }
    public IReadOnlyList<StatusOption> Options { get; } = [
        new(ProceduralEvidenceState.NotReviewed, "İncelenmedi"),
        new(ProceduralEvidenceState.Documented, "Belge ile kontrol edildi"),
        new(ProceduralEvidenceState.Deficiency, "Eksik / çelişkili"),
        new(ProceduralEvidenceState.NotApplicable, "Gerekçeli uygulanmaz")
    ];
    public SystematicRow(ProceduralCheck check, Action update) { Check = check; _update = update; }
    public ProceduralEvidenceState State { get => _state; set { _state = value; Changed(); _update(); } }
    public string Evrak { get => _evrak; set { _evrak = value; Changed(); _update(); } }
    public ProceduralEvidence Evidence() => new(Check.Id, State, Evrak);
    public void Clear() { _state = ProceduralEvidenceState.NotReviewed; _evrak = ""; Changed(nameof(State)); Changed(nameof(Evrak)); _update(); }
}

public sealed class SystematicsPage : ObservableViewModel
{
    public ProceduralWorkflow Workflow { get; }
    public string Title { get; }
    public string ButtonTitle => Workflow.Title;
    public string Description => Workflow.Description;
    public IReadOnlyList<ProceduralCheck> Steps => Workflow.Checks;
    public bool IsSearch => Workflow.Key == "search";
    public IReadOnlyList<NormDispute> NormRisks => IsSearch ? AdliSearchNormGate.Risks : [];
    private string _eventDate = "", _seizure = "", _submission = "", _decision = "";
    private string _normSummary = AdliSearchNormGate.Assess(null).Summary;
    private string _clockSummary = "CMK 127/3 belge saatleri girilmedi; hukuki sonuç üretilmedi.";
    public string EventDate { get => _eventDate; set { _eventDate = value; Changed(); } }
    public string SeizureTime { get => _seizure; set { _seizure = value; Changed(); } }
    public string SubmissionTime { get => _submission; set { _submission = value; Changed(); } }
    public string DecisionTime { get => _decision; set { _decision = value; Changed(); } }
    public string NormSummary { get => _normSummary; private set { _normSummary = value; Changed(); } }
    public string ClockSummary { get => _clockSummary; private set { _clockSummary = value; Changed(); } }
    public ICommand ReviewNormCommand { get; }
    public ICommand CompareClockCommand { get; }
    public ObservableCollection<SystematicRow> Rows { get; }
    private bool _opened, _diagram = true, _audit;
    private string _fileCode = "", _message = "Hukuki mevzuat tamlığı ve dosya uygunluğu otomatik doğrulanmaz.";
    public bool Opened { get => _opened; private set { _opened = value; Changed(); } }
    public bool ShowDiagram { get => _diagram; private set { _diagram = value; Changed(); } }
    public bool ShowAudit { get => _audit; private set { _audit = value; Changed(); } }
    public string FileCode { get => _fileCode; set { _fileCode = value; Changed(); } }
    public string Message { get => _message; private set { _message = value; Changed(); } }
    public bool HasReviewData => !string.IsNullOrWhiteSpace(FileCode) ||
        (IsSearch && (!string.IsNullOrWhiteSpace(EventDate) ||
        !string.IsNullOrWhiteSpace(SeizureTime) ||
        !string.IsNullOrWhiteSpace(SubmissionTime) ||
        !string.IsNullOrWhiteSpace(DecisionTime))) || Rows.Any(r =>
        r.State != ProceduralEvidenceState.NotReviewed || !string.IsNullOrWhiteSpace(r.Evrak));
    public string ReviewSummary {
        get {
            var s = ProceduralReviewEngine.Evaluate(Workflow, Rows.Select(x => x.Evidence()));
            return $"{s.Total} adım | belgeyle kontrol: {s.Documented} | eksik: {s.Deficiencies} | " +
                $"incelenmedi: {s.NotReviewed} | uygulanmaz: {s.NotApplicable} | gerekçesiz: {s.UnsupportedDeclarations}. " +
                "HUKUKİ UYGUNLUK ONAYI DEĞİLDİR.";
        }
    }
    public ICommand OpenCommand { get; }
    public ICommand DiagramCommand { get; }
    public ICommand AuditCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand ResetCommand { get; }
    public SystematicsPage(ProceduralWorkflow workflow, string title)
    {
        Workflow = workflow; Title = title;
        Rows = new ObservableCollection<SystematicRow>(workflow.Checks.Select(step =>
            new SystematicRow(step, () => Changed(nameof(ReviewSummary)))));
        OpenCommand = new RelayCommand(_ => { Opened = !Opened; if (Opened) Show(diagram: true); });
        DiagramCommand = new RelayCommand(_ => Show(diagram: true));
        AuditCommand = new RelayCommand(_ => Show(diagram: false));
        CopyCommand = new RelayCommand(_ => Copy());
        ResetCommand = new RelayCommand(_ => Reset());
        ReviewNormCommand = new RelayCommand(_ => ReviewNorm());
        CompareClockCommand = new RelayCommand(_ => CompareClock());
    }
    private void ReviewNorm()
    {
        if (!IsSearch) return;
        if (!DateOnly.TryParseExact(EventDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            NormSummary = "Olay tarihi yyyy-MM-dd biçiminde değil veya eksik. Sürüm ve iptal/YD riski AÇIK.";
            return;
        }
        NormSummary = AdliSearchNormGate.Assess(date).Summary +
            " Kaynakları ve birincil karar metinlerini insan doğrulamalı; resmî tamlık PASS yok.";
    }

    private void CompareClock()
    {
        if (!IsSearch) return;
        static bool ParseOptional(string text, out DateTimeOffset? moment)
        {
            moment = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (!SeizureClockAudit.TryParseLocalWithOffset(text.Trim(), out var parsed))
                return false;
            moment = parsed;
            return true;
        }
        if (!ParseOptional(SeizureTime, out var start) ||
            !ParseOptional(SubmissionTime, out var submitted) ||
            !ParseOptional(DecisionTime, out var decided))
        {
            ClockSummary = "Geçersiz tarih-saat; açık UTC farkı zorunlu (örn. 2026-11-20T14:30+03:00). Hesap YOK.";
            return;
        }
        ClockSummary = SeizureClockAudit.Compare(start, submitted, decided).Summary;
    }

    private void Show(bool diagram) { ShowDiagram = diagram; ShowAudit = !diagram; }
    private void Reset()
    {
        if (!HasReviewData) return;
        if (MessageBox.Show("RAM'deki geçici denetim işaretleri silinsin mi?",
            "Embasic Legal", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        foreach (var row in Rows) row.Clear();
        FileCode = "";
        if (IsSearch)
        {
            EventDate = "";
            SeizureTime = "";
            SubmissionTime = "";
            DecisionTime = "";
            NormSummary = AdliSearchNormGate.Assess(null).Summary;
            ClockSummary = "CMK 127/3 belge saatleri girilmedi; hukuki sonuç üretilmedi.";
        }
        Message = "Geçici kontroller temizlendi; otomatik hukuki karar yok.";
    }
    private void Copy()
    {
        if (MessageBox.Show("Dosya denetimi panoya kopyalanacak. Kişisel bilgi girmeyin; " +
            "pano erişimi başka uygulamalarda mümkün olabilir. Devam?",
            "Embasic Legal — Pano Gizliliği", MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var b = new StringBuilder();
        b.AppendLine("EMBASIC LEGAL SİSTEMATİK — İNSAN DENETİM TASLAĞI / HUKUKİ KARAR DEĞİLDİR");
        b.AppendLine(ButtonTitle);
        b.AppendLine("Kimliksiz dosya kodu: " + FileCode);
        b.AppendLine("Tarih: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        if (IsSearch)
        {
            b.AppendLine("Kullanıcı tarafından girilen olay tarihi: " + EventDate);
            b.AppendLine("Norm riski: " + NormSummary);
            b.AppendLine("CMK127/3 saat aritmetiği: " + ClockSummary);
            b.AppendLine("RESMÎ METİN / BİRİNCİL KARAR / HUKUKİ UYGUNLUK ONAYI: YOK");
        }
        foreach (var item in Rows) {
            b.AppendLine(item.Check.Id + " | " + item.Check.Phase + " | " + item.Check.Question);
            b.AppendLine("Kaynak kontrolü: " + item.Check.LegalReferences);
            b.AppendLine("Durum: " + item.Options.First(x => x.Code == item.State).DisplayName);
            b.AppendLine("Evrak/gerekçe: " + item.Evrak);
        }
        b.AppendLine("HUKUKİ UYGUNLUK OTOMATİK ONAYLANMADI. Olay tarihi ve mevzuat sürümü insan tarafından doğrulanmalıdır.");
        try { Clipboard.SetText(b.ToString()); Message = "Denetim panoya kopyalandı. Diske otomatik kayıt yapılmadı."; }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException)
        { Message = "Panoya kopyalanamadı: " + ex.Message; }
    }
}
