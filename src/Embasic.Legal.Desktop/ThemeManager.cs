using System;
using System.IO;
using System.Windows;

namespace Embasic.Legal.Desktop;

public static class ThemeManager
{
    private static readonly string PreferenceFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Embasic", "Legal", "theme.txt");
    public static bool IsDark { get; private set; } = true;

    public static void Initialize()
    {
        try { IsDark = File.ReadAllText(PreferenceFile).Trim() != "light"; }
        catch (IOException) { IsDark = true; }
        catch (UnauthorizedAccessException) { IsDark = true; }
        Apply(IsDark, persist: false);
    }

    public static void Toggle() => Apply(!IsDark, persist: true);

    private static void Apply(bool dark, bool persist)
    {
        var resource = new ResourceDictionary {
            Source = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative)
        };
        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(resource);
        IsDark = dark;
        if (!persist) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferenceFile)!);
            File.WriteAllText(PreferenceFile, dark ? "dark" : "light");
        }
        catch (IOException) { /* Keep the active theme if persistence fails. */ }
        catch (UnauthorizedAccessException) { /* Keep the active theme. */ }
    }
}
