using System.Windows;
using Notty.App.Services;
using Notty.App.ViewModels;
using Notty.Core.Services;

namespace Notty.App;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Simple manual composition — no DI container needed for V1.
        var settingsService = new SettingsService();
        var settings = settingsService.Load();

        var themeService = new ThemeService();
        themeService.EnsureDefaults();
        var themeManager = new ThemeManager(themeService);
        themeManager.ApplyByName(settings.Theme); // theme the UI before the window appears

        _mainViewModel = new MainViewModel(
            settingsService,
            new WorkspaceScanner(),
            new WorkspaceService(),
            new NoteService(),
            new DialogService(),
            themeManager);

        var window = new MainWindow { DataContext = _mainViewModel };
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Guarantee the autosave debounce is flushed before we quit.
        _mainViewModel?.FlushPendingSaves();
        base.OnExit(e);
    }
}
