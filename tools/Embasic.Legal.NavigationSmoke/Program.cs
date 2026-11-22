using Embasic.Legal.Core;
using Embasic.Legal.Desktop.ViewModels;

// Actual Windows STA ViewModel commands. No visible GUI or physical user acceptance.
internal static class Program
{
    private static int _checks;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
        Console.WriteLine("PASS " + message);
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            var app = new MainViewModel();
            Require(app.CurrentPage is DashboardViewModel, "Application starts on dashboard");
            Require(((DashboardViewModel)app.CurrentPage).LearningSummary.Contains("47 konu"),
                "Dashboard counts central topics without alternative solutions");
            var root = app.NavigationItems.Single(x => x.Key == "learning");
            Require(root.Label == "Ceza Muhakemesi Hukuku" && root.HasChildren && root.Children!.Count == 13,
                "Renamed centre owns all 13 child categories");
            Require(!app.NavigationItems.Any(x => x.Key.StartsWith("cmk:") ||
                new[] { "detention", "supervision", "search" }.Contains(x.Key)),
                "No duplicated CMK categories or procedure tools at menu root");
            Require(app.NavigationItems.Single(x => x.Key == "alternatives").Children is null,
                "Alternative solutions remain a separate top-level section");
            app.NavigateCommand.Execute("learning");
            var hub = app.CurrentPage as CmkLearningHubViewModel;
            Require(hub is not null && hub.Title == "Ceza Muhakemesi Hukuku"
                && app.CurrentTitle == hub.Title, "Application opens renamed consolidated centre");
            foreach (var category in CmkCurriculum.Categories)
            {
                app.NavigateCommand.Execute("cmk:" + category.Key);
                Require(ReferenceEquals(app.CurrentPage, hub) && hub!.SelectedCategory.Key == category.Key
                    && category.TopicKeys.Contains(hub.SelectedTopic.Key) && !hub.IsAlternativeMode,
                    "Child category opens central topic: " + category.Key);
            }
            app.NavigateCommand.Execute("alternatives");
            Require(ReferenceEquals(app.CurrentPage, hub) && hub!.IsAlternativeMode
                && hub.Categories.Count == 1 && hub.Topics.Count == 3 && hub.SelectedTopic.Key == "cmk250"
                && app.CurrentTitle == "Alternatif Çözümler", "Alternatives contain only three separate dedicated lessons");
            hub!.Checklist[0].Reviewed = true;
            app.NavigateCommand.Execute("home");
            app.NavigateCommand.Execute("cmk:sorusturma");
            app.NavigateCommand.Execute("alternatives");
            Require(ReferenceEquals(app.CurrentPage, hub) && hub.Checklist[0].Reviewed,
                "Cross-section navigation preserves learning session marks");
            hub.OpenTopic("cmk250-253");
            Require(hub.SelectedTopic.Key == "cmk250", "Old combined overview redirects to one dedicated topic");
            Require(hub.LearningProgress.Contains("/ 100"), "Global denominator excludes duplicate overview");
            app.NavigateCommand.Execute("learning");
            Require(!hub.IsAlternativeMode && hub.Categories.Count == 13,
                "Centre entry exits alternative-only scope");
            app.NavigateCommand.Execute("detention");
            var detention = hub.Procedure;
            Require(ReferenceEquals(app.CurrentPage, hub) && detention?.Workflow.Key == "detention"
                && hub.SelectedTopic.Key == "cmk100-108" && hub.SelectedTabIndex == 6,
                "Detention tool is inside its canonical lesson");
            detention!.FileCode = "SYNTHETIC-SESSION";
            app.NavigateCommand.Execute("supervision");
            var supervision = hub.Procedure;
            Require(ReferenceEquals(app.CurrentPage, hub) && supervision?.Workflow.Key == "supervision"
                && hub.SelectedTopic.Key == "cmk109-115", "Judicial control tool is inside its canonical lesson");
            app.NavigateCommand.Execute("detention");
            Require(ReferenceEquals(hub.Procedure, detention) && detention.FileCode == "SYNTHETIC-SESSION"
                && app.HasPendingSystematicData, "Consolidation preserves procedure object and pending-data close guard");
            app.NavigateCommand.Execute("search");
            var search = hub.Procedure;
            Require(ReferenceEquals(app.CurrentPage, hub) && search?.IsSearch == true,
                "Search and seizure tool is embedded in the centre");
            search!.SeizureEvidence = "SYNTHETIC-EVIDENCE";
            hub.OpenTopic("cmk134");
            Require(ReferenceEquals(hub.Procedure, search) && search.SeizureEvidence == "SYNTHETIC-EVIDENCE",
                "Related search topics share a single procedure tool");
            hub.OpenTopic("cmk1-2");
            Require(!hub.HasProcedure && hub.SelectedTabIndex == 0,
                "Unrelated topic hides procedure tab and resets selection");
            hub.OpenTopic("cmk251-252");
            Require(!hub.IsAlternativeMode && hub.SelectedCategory.Key == "ozel",
                "Simplified trial remains inside criminal procedure law");
            Require(hub.TopicStatus.Contains("NOT_VERIFIED"), "Consolidation does not certify current legislation");
            detention.FileCode = "";
            search.SeizureEvidence = "";
            app.SecureCloseNotesOnExit();
            Console.WriteLine($"WINDOWS_STA_NAVIGATION_PASS {_checks} checks; PHYSICAL_GUI_NOT_RUN");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
