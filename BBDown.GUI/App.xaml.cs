using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BBDown.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply system theme with Mica backdrop
        ApplicationThemeManager.Apply(
            ApplicationTheme.Unknown,
            WindowBackdropType.Mica,
            true
        );

        // Create and show the main window
        var mainWindow = new MainWindow();
        mainWindow.Show();

        // Watch for system theme changes
        SystemThemeWatcher.Watch(
            mainWindow,
            WindowBackdropType.Mica,
            true
        );
    }
}
