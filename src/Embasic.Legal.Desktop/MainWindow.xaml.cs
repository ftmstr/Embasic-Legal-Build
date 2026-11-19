using System.Windows;
using System.ComponentModel;
using Embasic.Legal.Desktop.ViewModels;

namespace Embasic.Legal.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && !vm.FlushNotesOnExit())
        {
            e.Cancel = true;
            MessageBox.Show("Kaydedilmemiş madde notu var. Kaydetmeden çıkış engellendi.",
                "Embasic Legal", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        base.OnClosing(e);
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e) => ThemeManager.Toggle();

    private void OnDetach(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model) return;
        new ToolWindow { Title = model.CurrentTitle + " — Embasic Legal", DataContext = model.CurrentPage,
            Owner = this }.Show();
    }
}
