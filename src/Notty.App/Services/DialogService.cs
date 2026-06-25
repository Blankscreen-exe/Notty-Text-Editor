using System.IO;
using System.Windows;
using Microsoft.Win32;
using Notty.App.Views;

namespace Notty.App.Services;

/// <summary>Thin wrapper over Win32 dialogs and message boxes, so view models stay testable-ish.</summary>
public sealed class DialogService
{
    /// <summary>Shows a folder picker. Returns the selected path, or null if cancelled.</summary>
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    /// <summary>
    /// Prompts for a folder location then a new folder name (used by "Create New Workspace").
    /// Returns the created folder path, or null if cancelled.
    /// </summary>
    public string? CreateFolder(string parent, string folderName)
    {
        var path = Path.Combine(parent, folderName);
        Directory.CreateDirectory(path);
        return path;
    }

    public void ShowError(string message) =>
        MessageBox.Show(OwnerWindow, message, "Notty", MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowInfo(string message, string title = "Notty") =>
        MessageBox.Show(OwnerWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    /// <summary>Yes/No confirmation. Returns true if the user chose Yes.</summary>
    public bool Confirm(string message, string title = "Notty") =>
        MessageBox.Show(OwnerWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;

    /// <summary>Prompts for a name. Returns the trimmed value, or null if cancelled/empty.</summary>
    public string? PromptForName(string title, string prompt, string initialValue = "") =>
        InputDialog.Prompt(OwnerWindow, title, prompt, initialValue);

    /// <summary>Opens the Settings window modally.</summary>
    public void ShowSettings(object viewModel)
    {
        var window = new SettingsWindow { Owner = OwnerWindow, DataContext = viewModel };
        window.ShowDialog();
    }

    private static Window OwnerWindow => Application.Current.MainWindow;
}
