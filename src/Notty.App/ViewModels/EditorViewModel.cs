using System.IO;
using System.Windows.Threading;
using Notty.App.Mvvm;
using Notty.Core.Services;

namespace Notty.App.ViewModels;

/// <summary>Whether the open document is saved, has pending edits, or is mid-save.</summary>
public enum SaveState { Saved, Modified, Saving }

/// <summary>
/// Holds the currently open document and its save lifecycle. When <see cref="AutoSave"/> is on,
/// edits are written 500ms after typing stops; either way changes are flushed when switching files
/// or closing. The current <see cref="Status"/> is surfaced in the status bar.
/// </summary>
public sealed class EditorViewModel : ObservableObject
{
    private static readonly TimeSpan AutosaveDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan SavingFlashDelay = TimeSpan.FromMilliseconds(350);

    private readonly NoteService _notes;
    private readonly DispatcherTimer _autosaveTimer;
    private readonly DispatcherTimer _savingFlashTimer;
    private readonly DispatcherTimer _relativeTimer;
    private readonly Action<string> _onSaved;

    private string? _currentFilePath;
    private string _text = string.Empty;
    private bool _isDirty;
    private bool _suppressDirty;
    private bool _autoSave = true;
    private SaveState _status = SaveState.Saved;
    private DateTimeOffset? _lastSavedAt;

    public EditorViewModel(NoteService notes, Action<string> onSaved)
    {
        _notes = notes;
        _onSaved = onSaved;

        _autosaveTimer = new DispatcherTimer { Interval = AutosaveDelay };
        _autosaveTimer.Tick += (_, _) => SaveNow();

        // Holds the "Saving…" label briefly after a (near-instant) local write, then flips to "Saved".
        _savingFlashTimer = new DispatcherTimer { Interval = SavingFlashDelay };
        _savingFlashTimer.Tick += (_, _) => { _savingFlashTimer.Stop(); Status = SaveState.Saved; };

        // Keeps the relative "Saved 43 sec ago" label current. Runs only while showing a saved time.
        _relativeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _relativeTimer.Tick += (_, _) => OnPropertyChanged(nameof(SaveStatusText));
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

    /// <summary>When true, edits autosave 500ms after typing stops. When false, saving is manual (Ctrl+S).</summary>
    public bool AutoSave
    {
        get => _autoSave;
        set
        {
            _autoSave = value;
            if (!value)
                _autosaveTimer.Stop(); // cancel any pending idle save; explicit saves still work
        }
    }

    /// <summary>Current save state of the open document.</summary>
    public SaveState Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(SaveStatusText));
                OnPropertyChanged(nameof(HasUnsavedChanges));

                if (value == SaveState.Saved && _lastSavedAt is not null)
                    _relativeTimer.Start();
                else
                    _relativeTimer.Stop();
            }
        }
    }

    /// <summary>True while there are edits not yet written (for status-bar emphasis).</summary>
    public bool HasUnsavedChanges => _status == SaveState.Modified;

    /// <summary>Human-readable save status for the status bar.</summary>
    public string SaveStatusText => _status switch
    {
        SaveState.Saving => "Saving…",
        SaveState.Modified => "Unsaved changes",
        _ => _lastSavedAt is { } at ? $"Saved {Relative(at)}" : string.Empty,
    };

    /// <summary>Turns a save time into a relative phrase ("just now", "43 sec ago", "2 mins ago").</summary>
    private static string Relative(DateTimeOffset at)
    {
        var span = DateTimeOffset.Now - at;
        if (span < TimeSpan.FromSeconds(5))
            return "just now";
        if (span < TimeSpan.FromMinutes(1))
            return $"{(int)span.TotalSeconds} sec ago";
        if (span < TimeSpan.FromHours(1))
        {
            var m = (int)span.TotalMinutes;
            return $"{m} min{(m == 1 ? "" : "s")} ago";
        }
        if (span < TimeSpan.FromDays(1))
        {
            var h = (int)span.TotalHours;
            return $"{h} hr{(h == 1 ? "" : "s")} ago";
        }

        return at.ToString("HH:mm");
    }

    /// <summary>The editor text. Setting it from user input marks the document dirty.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value) && !_suppressDirty)
            {
                _isDirty = true;
                _savingFlashTimer.Stop();
                Status = SaveState.Modified;

                if (_autoSave)
                {
                    _autosaveTimer.Stop();
                    _autosaveTimer.Start();
                }
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
        _lastSavedAt = null; // a freshly opened file has no "saved … ago" yet
        _savingFlashTimer.Stop();
        Status = SaveState.Saved;
    }

    /// <summary>Writes pending changes to disk immediately. Safe to call when nothing is dirty.</summary>
    public void SaveNow()
    {
        _autosaveTimer.Stop();
        if (!_isDirty || _currentFilePath is null)
            return;

        Status = SaveState.Saving;
        _notes.WriteText(_currentFilePath, _text);
        _isDirty = false;
        _lastSavedAt = DateTimeOffset.Now;
        _onSaved(_currentFilePath);

        // Briefly show "Saving…", then flip to "Saved" (the write itself is effectively instant).
        _savingFlashTimer.Stop();
        _savingFlashTimer.Start();
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
        _lastSavedAt = null;
        _savingFlashTimer.Stop();
        Status = SaveState.Saved;
    }
}
