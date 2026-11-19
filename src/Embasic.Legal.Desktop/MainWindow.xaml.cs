using System.Windows;
using Embasic.Legal.Desktop.ViewModels;

namespace Embasic.Legal.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e) => ThemeManager.Toggle();

    private void OnDetach(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model) return;
        new ToolWindow { Title = model.CurrentTitle + " — Embasic Legal", DataContext = model.CurrentPage,
            Owner = this }.Show();
    }
}
