using Embasic.Legal.Core;
using Embasic.Legal.Desktop.ViewModels;

// Real Windows STA ViewModel command integration. No visible GUI, vault opening,
// disk writes, network, legal decision or claim of physical user acceptance.
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
            Require(((DashboardViewModel)app.CurrentPage).LearningSummary.Contains("51 konu"),
                "Dashboard uses current curriculum counts");
            app.NavigateCommand.Execute("learning");
            var hub = app.CurrentPage as CmkLearningHubViewModel;
            Require(hub is not null, "Main application opens actual learning ViewModel");
            foreach (var category in CmkCurriculum.Categories)
            {
                app.NavigateCommand.Execute("cmk:" + category.Key);
                Require(ReferenceEquals(app.CurrentPage, hub) && hub!.SelectedCategory.Key == category.Key
                    && category.TopicKeys.Contains(hub.SelectedTopic.Key),
                    "Sidebar category opens its own topic: " + category.Key);
            }
            app.NavigateCommand.Execute("alternatives");
            Require(ReferenceEquals(app.CurrentPage, hub) && hub!.SelectedTopic.Key == "cmk250-253"
                && hub.SelectedCategory.Key == "ozel", "Existing alternatives menu opens relevant lesson");
            hub!.Checklist[0].Reviewed = true;
            app.NavigateCommand.Execute("home");
            app.NavigateCommand.Execute("cmk:sorusturma");
            app.NavigateCommand.Execute("alternatives");
            Require(ReferenceEquals(app.CurrentPage, hub) && hub.Checklist[0].Reviewed,
                "Home and category navigation preserve learning session marks");
            Require(hub.LearningProgress.Contains("/ 102"), "Global quiz denominator remains 102");
            app.NavigateCommand.Execute("detention");
            Require(app.CurrentPage is SystematicsPage, "Existing detention tool remains available");
            app.NavigateCommand.Execute("supervision");
            Require(app.CurrentPage is SystematicsPage, "Existing judicial control tool remains available");
            app.NavigateCommand.Execute("search");
            Require(app.CurrentPage is SystematicsPage, "Existing search and seizure tool remains available");
            Require(hub.TopicStatus.Contains("NOT_VERIFIED"), "Source status never becomes legal approval");
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
