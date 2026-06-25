using System.IO;
using System.Windows.Threading;
using Notty.App.Mvvm;
using Notty.Core.Services;

namespace Notty.App.ViewModels;

/// <summary>
/// Holds the currently open document and implements autosave:
/// 500ms after the user stops typing, when switching files, and on close.
/// </summary>
public sealed class EditorViewModel : ObservableObject
{
    private static readonly TimeSpan AutosaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly NoteService _notes;
    private readonly DispatcherTimer _autosaveTimer;
    private readonly Action<string> _onSaved;

    private string? _currentFilePath;
    private string _text = string.Empty;
    private bool _isDirty;
    private bool _suppressDirty;

    public EditorViewModel(NoteService notes, Action<string> onSaved)
    {
        _notes = notes;
        _onSaved = onSaved;
        _autosaveTimer = new DispatcherTimer { Interval = AutosaveDelay };
        _autosaveTimer.Tick += (_, _) => SaveNow();
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(HasDocument));
                OnPropertyChanged(nameof(CurrentFileName));
            }
        }
    }

    public string CurrentFileName =>
        _currentFilePath is null ? string.Empty : Path.GetFileName(_currentFilePath);

    public bool HasDocument => _currentFilePath is not null;

    /// <summary>The editor text. Setting it from user input schedules an autosave.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value) && !_suppressDirty)
            {
                _isDirty = true;
                _autosaveTimer.Stop();
                _autosaveTimer.Start();
            }
        }
    }

    /// <summary>Saves the current document (if dirty), then loads <paramref name="path"/>.</summary>
    public void OpenFile(string path)
    {
        SaveNow();

        _suppressDirty = true;
        try
        {
            Text = _notes.ReadText(path);
        }
        finally
        {
            _suppressDirty = false;
        }

        CurrentFilePath = path;
        _isDirty = false;
    }

    /// <summary>Writes pending changes to disk immediately. Safe to call when nothing is dirty.</summary>
    public void SaveNow()
    {
        _autosaveTimer.Stop();
        if (!_isDirty || _currentFilePath is null)
            return;

        _notes.WriteText(_currentFilePath, _text);
        _isDirty = false;
        _onSaved(_currentFilePath);
    }

    /// <summary>Closes the active document, flushing any pending save first.</summary>
    public void Close()
    {
        SaveNow();
        ClearDocument();
    }

    /// <summary>Closes the active document and drops pending changes (used when the file is being deleted).</summary>
    public void CloseWithoutSaving()
    {
        _autosaveTimer.Stop();
        _isDirty = false;
        ClearDocument();
    }

    private void ClearDocument()
    {
        _suppressDirty = true;
        Text = string.Empty;
        _suppressDirty = false;
        CurrentFilePath = null;
    }
}
