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
    public ObservableCollection<SystematicRow> Rows { get; }
    private bool _opened, _diagram = true, _audit;
    private string _fileCode = "", _message = "Hukuki mevzuat tamlığı ve dosya uygunluğu otomatik doğrulanmaz.";
    public bool Opened { get => _opened; private set { _opened = value; Changed(); } }
    public bool ShowDiagram { get => _diagram; private set { _diagram = value; Changed(); } }
    public bool ShowAudit { get => _audit; private set { _audit = value; Changed(); } }
    public string FileCode { get => _fileCode; set { _fileCode = value; Changed(); } }
    public string Message { get => _message; private set { _message = value; Changed(); } }
    public bool HasReviewData => !string.IsNullOrWhiteSpace(FileCode) || Rows.Any(r =>
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
    }
    private void Show(bool diagram) { ShowDiagram = diagram; ShowAudit = !diagram; }
    private void Reset()
    {
        if (!HasReviewData) return;
        if (MessageBox.Show("RAM'deki geçici denetim işaretleri silinsin mi?",
            "Embasic Legal", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        foreach (var row in Rows) row.Clear();
        FileCode = "";
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
