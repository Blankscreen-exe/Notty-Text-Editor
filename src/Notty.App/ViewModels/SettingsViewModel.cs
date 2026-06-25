using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using Notty.App.Mvvm;
using Notty.Core.Models;

namespace Notty.App.ViewModels;

/// <summary>
/// Backs the Settings window. Reads/writes the shared <see cref="AppSettings"/> and applies each
/// change immediately via callbacks supplied by <see cref="MainViewModel"/>, which owns persistence,
/// the editor, and the tree.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly Action<string> _applyTheme;
    private readonly Action _applyEditor;
    private readonly Action _applyFiles;

    public SettingsViewModel(
        AppSettings settings,
        IEnumerable<string> themeNames,
        Action<string> applyTheme,
        Action applyEditor,
        Action applyFiles)
    {
        _settings = settings;
        _applyTheme = applyTheme;
        _applyEditor = applyEditor;
        _applyFiles = applyFiles;

        Themes = new ObservableCollection<string>(themeNames);
        FontFamilies = new ObservableCollection<string>(
            Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        FontSizes = new ObservableCollection<double> { 10, 11, 12, 13, 14, 16, 18, 20, 24, 28 };
        TabWidths = new ObservableCollection<int> { 2, 4, 8 };
    }

    public ObservableCollection<string> Themes { get; }
    public ObservableCollection<string> FontFamilies { get; }
    public ObservableCollection<double> FontSizes { get; }
    public ObservableCollection<int> TabWidths { get; }

    // ---- Appearance ----

    public string SelectedTheme
    {
        get => _settings.Theme;
        set
        {
            if (value is not null && !string.Equals(value, _settings.Theme, StringComparison.OrdinalIgnoreCase))
            {
                _applyTheme(value); // updates _settings.Theme, persists, swaps brushes
                OnPropertyChanged();
            }
        }
    }

    // ---- Editor ----

    public string SelectedFontFamily
    {
        get => _settings.EditorFontFamily;
        set => SetEditor(value, _settings.EditorFontFamily, v => _settings.EditorFontFamily = v);
    }

    public double SelectedFontSize
    {
        get => _settings.EditorFontSize;
        set => SetEditor(value, _settings.EditorFontSize, v => _settings.EditorFontSize = v);
    }

    public bool WordWrap
    {
        get => _settings.EditorWordWrap;
        set => SetEditor(value, _settings.EditorWordWrap, v => _settings.EditorWordWrap = v);
    }

    public bool ShowLineNumbers
    {
        get => _settings.EditorShowLineNumbers;
        set => SetEditor(value, _settings.EditorShowLineNumbers, v => _settings.EditorShowLineNumbers = v);
    }

    public int SelectedTabWidth
    {
        get => _settings.EditorTabWidth;
        set => SetEditor(value, _settings.EditorTabWidth, v => _settings.EditorTabWidth = v);
    }

    public bool HighlightCurrentLine
    {
        get => _settings.EditorHighlightCurrentLine;
        set => SetEditor(value, _settings.EditorHighlightCurrentLine, v => _settings.EditorHighlightCurrentLine = v);
    }

    // ---- Saving ----

    public bool AutoSave
    {
        get => _settings.AutoSave;
        set => SetEditor(value, _settings.AutoSave, v => _settings.AutoSave = v);
    }

    // ---- Files ----

    public bool ShowUnsupportedFiles
    {
        get => _settings.ShowUnsupportedFiles;
        set
        {
            if (value != _settings.ShowUnsupportedFiles)
            {
                _settings.ShowUnsupportedFiles = value;
                _applyFiles();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Writes an editor setting if it changed, then applies it live.</summary>
    private void SetEditor<T>(T value, T current, Action<T> assign, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(value, current))
            return;

        assign(value);
        _applyEditor();
        OnPropertyChanged(propertyName);
    }
}
